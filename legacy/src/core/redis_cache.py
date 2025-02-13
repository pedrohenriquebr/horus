import redis
import json
import logging
import zlib
import os
from typing import Any, Dict, List, Optional
from datetime import datetime, timedelta

logger = logging.getLogger(__name__)

class RedisCache:
    def __init__(self):
        """Inicializa conexão com Redis"""
        self.redis = redis.Redis(
            host=os.getenv('REDIS_HOST', 'localhost'),
            port=int(os.getenv('REDIS_PORT', 6379)),
            db=int(os.getenv('REDIS_DB', 0)),
            password=os.getenv('REDIS_PASSWORD'),
            decode_responses=True
        )
        # TTLs específicos para cada tipo de cache (em segundos)
        self.ttl_config = {
            'embedding': 60 * 30,      # 30 minutos para embeddings
            'working_memory': 60 * 2,  # 2 minutos para memória de trabalho
            'chat_history': 60 * 10,   # 10 minutos para histórico de chat
            'llm_response': 60 * 5,    # 5 minutos para respostas do LLM
            'memory': 60 * 15,         # 15 minutos para memórias
            'search_result': 60 * 60 * 24,  # 24 horas para resultados de busca
        }

    def _get_user_key(self, key_type: str, user_id: str) -> str:
        """Gera chave para o Redis no formato 'horus:{tipo}:{user_id}'"""
        return f"horus:{key_type}:{user_id}"

    def _compress(self, data: str) -> bytes:
        """Comprime dados para economizar espaço"""
        return zlib.compress(json.dumps(data).encode())

    def _decompress(self, data: bytes) -> Any:
        """Descomprime dados"""
        return json.loads(zlib.decompress(data).decode())

    def _parse_memory_string(self, memory_string: str) -> Dict:
        """Parse memory string in different formats."""
        try:
            timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
            memory_type = "memory"
            content = memory_string

            # Tenta extrair timestamp e tipo da memória
            if isinstance(memory_string, str):
                # Formato 1: JSON string
                try:
                    data = json.loads(memory_string)
                    if isinstance(data, dict):
                        timestamp = data.get('timestamp', timestamp)
                        memory_type = data.get('type', memory_type)
                        content = data.get('content', content)
                        return {
                            "timestamp": timestamp,
                            "type": memory_type,
                            "content": content
                        }
                except json.JSONDecodeError:
                    pass

                # Formato 2: "content (Registrado em timestamp)"
                if " (Registrado em " in memory_string:
                    parts = memory_string.rsplit(" (Registrado em ", 1)
                    if len(parts) == 2:
                        content = parts[0]
                        timestamp = parts[1].rstrip(")")
                        if "Usuário:" in content:
                            memory_type = "user_message"
                        elif "Bot:" in content:
                            memory_type = "bot_message"
                        return {
                            "timestamp": timestamp,
                            "type": memory_type,
                            "content": content
                        }

                # Formato 3: Mensagens do usuário/bot
                if memory_string.startswith("Usuário:"):
                    memory_type = "user_message"
                    content = memory_string[8:].strip()  # Remove "Usuário: "
                elif memory_string.startswith("Bot:"):
                    memory_type = "bot_message"
                    content = memory_string[4:].strip()  # Remove "Bot: "

            return {
                "timestamp": timestamp,
                "type": memory_type,
                "content": content
            }
        except Exception as e:
            logger.error(f"Error parsing memory string: {memory_string}, error: {str(e)}")
            return {
                "timestamp": datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                "type": "unknown",
                "content": str(memory_string)
            }

    def get_active_context(self) -> List[Dict]:
        """Get the current active context from Redis."""
        try:
            # Busca todas as chaves de memória de trabalho
            keys = self.redis.keys("horus:memory:*")
            print('keys:', keys)
            context = []
            
            for key in keys:
                # Check the type of the key
                key_type = self.redis.type(key)  # Decode bytes to string
                print(f'Key {key} type: {key_type}')
                
                if key_type == 'list':
                    # If it's a list, get all elements
                    data_list = self.redis.lrange(key, 0, -1)
                    print(f'List data: {data_list}')
                    for data in data_list:
                        try:
                            if isinstance(data, bytes):
                                data = data.decode('utf-8')
                            print(f'Processing list item: {data}')
                            # Converte a string em dict
                            context_data = self._parse_memory_string(data)
                            context.append(context_data)
                        except Exception as e:
                            logger.error(f"Error processing list item: {data}, error: {str(e)}")
                            continue
                elif key_type == 'string':
                    # If it's a string, get it directly
                    data = self.redis.get(key)
                    try:
                        if isinstance(data, bytes):
                            data = data.decode('utf-8')
                        # Converte a string em dict
                        context_data = self._parse_memory_string(data)
                        context.append(context_data)
                    except Exception as e:
                        logger.error(f"Error processing string: {data}, error: {str(e)}")
                        continue
                else:
                    logger.warning(f"Unexpected Redis type for key {key}: {key_type}")
            
            print("Final context:", context)
            # Ordena por timestamp se disponível
            context.sort(key=lambda x: x.get('timestamp', ''), reverse=True)
            return context
        except Exception as e:
            logger.error(f"Error getting active context from Redis: {e}")
            return []

    # Cache de Embeddings
    def get_embedding(self, text: str) -> Optional[List[float]]:
        """Recupera embedding do cache"""
        key = f"horus:embedding:{hash(text)}"
        data = self.redis.get(key)
        return json.loads(data) if data else None

    def set_embedding(self, text: str, embedding: List[float]):
        """Armazena embedding no cache"""
        key = f"horus:embedding:{hash(text)}"
        self.redis.set(key, json.dumps(embedding), ex=self.ttl_config['embedding'])

    # Working Memory
    def get_working_memory(self, user_id: str) -> List[str]:
        """Recupera memória de trabalho do usuário"""
        key = self._get_user_key("working_memory", user_id)
        data = self.redis.lrange(key, 0, -1)
        logger.info(f"[Redis] Memória de trabalho do usuário {user_id} usando key {key}: {data}")
        return data if data else []

    def update_working_memory(self, user_id: str, memories: List[str], max_size: int = 20):
        """Atualiza memória de trabalho do usuário"""
        key = self._get_user_key("working_memory", user_id)
        pipe = self.redis.pipeline()
        pipe.delete(key)
        if memories:
            pipe.rpush(key, *memories[-max_size:])
            pipe.expire(key, self.ttl_config['working_memory'])
        pipe.execute()

    # Chat History
    def get_chat_history(self, user_id: str, limit: int = 10) -> List[Dict]:
        """Recupera histórico de chat do usuário"""
        key = self._get_user_key("chat_history", user_id)
        data = self.redis.lrange(key, 0, limit - 1)
        return [json.loads(item) for item in data] if data else []

    def add_chat_message(self, user_id: str, message: Dict):
        """Adiciona mensagem ao histórico de chat"""
        key = self._get_user_key("chat_history", user_id)
        self.redis.lpush(key, json.dumps(message))
        self.redis.ltrim(key, 0, 9)  # Mantém apenas as últimas 10 mensagens
        self.redis.expire(key, self.ttl_config['chat_history'])

    def clear_chat_history(self, user_id: str):
        """Limpa o histórico de chat do usuário no Redis"""
        key = self._get_user_key("chat_history", user_id)
        self.redis.delete(key)

    # Memory Storage
    def get_memories(self, user_id: str) -> List[str]:
        """Recupera memórias do usuário"""
        key = self._get_user_key("memory", user_id)
        data = self.redis.lrange(key, 0, -1)
        return [item for item in data] if data else []

    def add_memory(self, user_id: str, memory: str):
        """Adiciona uma nova memória"""
        key = self._get_user_key("memory", user_id)
        self.redis.lpush(key, memory)
        self.redis.expire(key, self.ttl_config['memory'])

    def update_memories(self, user_id: str, memories: List[str], max_size: int = 20):
        """Sincroniza memórias do Supabase com Redis"""
        key = self._get_user_key("memory", user_id)
        pipe = self.redis.pipeline()
        pipe.delete(key)
        if memories:
            pipe.lpush(key, *memories[-max_size:])
            pipe.expire(key, self.ttl_config['memory'])
        pipe.execute()

    # LLM Response Cache
    def get_llm_response(self, prompt: str) -> Optional[str]:
        """Recupera resposta do LLM do cache"""
        key = f"horus:llm_response:{hash(prompt)}"
        data = self.redis.get(key)
        return self._decompress(data) if data else None

    def set_llm_response(self, prompt: str, response: str):
        """Armazena resposta do LLM no cache"""
        key = f"horus:llm_response:{hash(prompt)}"
        self.redis.set(key, self._compress(response), ex=self.ttl_config['llm_response'])

    # Search Result Cache
    def get_search_result(self, url: str) -> Optional[str]:
        """Recupera resultado de busca do cache"""
        key = f"horus:search_result:{hash(url)}"
        data = self.redis.get(key)
        return self._decompress(data) if data else None

    def set_search_result(self, url: str, content: str):
        """Armazena resultado de busca no cache"""
        key = f"horus:search_result:{hash(url)}"
        self.redis.set(key, self._compress(content), ex=self.ttl_config['search_result'])
