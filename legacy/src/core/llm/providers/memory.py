import logging
from typing import Dict, List, Any, Optional
from datetime import datetime
from ..base import MemoryProvider
from ...supabase_rag import SupabaseRAG
from ...redis_cache import RedisCache

logger = logging.getLogger(__name__)

class RAGMemoryProvider(MemoryProvider):
    """Implementação de memória usando RAG (Supabase + Redis)"""
    def __init__(self, rag: SupabaseRAG, cache: RedisCache):
        self.rag = rag
        self.cache = cache
        self.max_working_memory = 30

    def store_memory(self, text: str, user_info: Dict[str, Any]) -> bool:
        """Armazena uma memória no Supabase e Redis"""
        try:
            metadata = {
                'type': 'memory',
                'user_id': user_info.get('id'),
                'timestamp': datetime.now().isoformat()
            }
            
            # Adiciona no Supabase
            result = self.rag.add_document(
                content=text,
                metadata=metadata
            )

            # Se sucesso no Supabase, adiciona no Redis
            if result:
                memory = {
                    'content': text,
                    'metadata': metadata
                }
                self.cache.add_memory(
                    user_info.get('id'),
                    f"{text} (Registrado em: {memory['metadata']['timestamp']})"
                )
                
                # Atualiza memória de trabalho
                self.update_working_memory(text, user_info)
                return True
                
        except Exception as e:
            logger.error(f"Erro ao armazenar memória: {e}")
        return False

    def get_memories(self, user_info: Dict[str, Any]) -> List[str]:
        """Recupera memórias do usuário"""
        try:
            return self.cache.get_memories(user_info.get('id'))
        except Exception as e:
            logger.error(f"Erro ao recuperar memórias: {e}")
            return []

    def update_working_memory(self, query: str, user_info: Dict[str, Any]) -> None:
        """Atualiza a memória de trabalho com base no contexto atual"""
        try:
            # Busca memórias similares
            memories = self.rag.search_similar(query, limit=self.max_working_memory)
            
            # Filtra apenas memórias do usuário atual
            relevant_memories = [
                mem for mem in memories 
                if mem.get('metadata', {}).get('type') == 'memory' 
                and str(mem.get('metadata', {}).get('user_id')) == str(user_info.get('id'))
            ]
            
            # Formata memórias
            formatted_memories = [
                f"{mem['content']} (Registrado em: {mem['metadata']['timestamp']})"
                for mem in relevant_memories
            ]
            
            # Atualiza no Redis
            self.cache.update_memories(
                user_info.get('id'),
                formatted_memories,
                self.max_working_memory
            )
            
        except Exception as e:
            logger.error(f"Erro ao atualizar memória de trabalho: {e}")

    def get_context(self, query: str) -> str:
        """Recupera informações contextuais gerais (não-memórias)"""
        logger.debug(f"Buscando contexto geral para query: {query}")
        try:
            # Busca documentos similares
            context = self.rag.get_context(query)
            
            return context
        except Exception as e:
            logger.error(f"Erro ao recuperar contexto: {e}")
            return []