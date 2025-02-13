from supabase import create_client, Client
import os
import logging
import json
from typing import List, Dict, Any
import requests
from datetime import datetime
import time
import requests.exceptions
import random
from sentence_transformers import SentenceTransformer

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)
file_handler = logging.FileHandler('supabase_rag.log', mode='w')
file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
logger.addHandler(file_handler)

class SupabaseRAG:
    def __init__(self, redis_cache):
        # Validate environment variables
        supabase_url = os.getenv('SUPABASE_URL')
        supabase_key = os.getenv('SUPABASE_KEY')
        
        if not all([supabase_url, supabase_key]):
            raise ValueError("Missing required environment variables: SUPABASE_URL and/or SUPABASE_KEY")
        
        try:
            self.supabase = create_client(supabase_url, supabase_key)
            logger.info("Verificando conexão com Supabase...")
            self.check_connection()
        except Exception as e:
            raise ConnectionError(f"Failed to initialize Supabase client: {e}")
            
        self.hf_api_key = os.getenv('HF_API_KEY')
        self.hf_api_url = "https://api-inference.huggingface.co/pipeline/feature-extraction/sentence-transformers/all-MiniLM-L6-v2"
        self.model_name = "sentence-transformers/all-MiniLM-L6-v2"
        self.local_model = None  # Lazy loading do modelo local
        
        if not self.hf_api_key:
            raise ValueError("HF_API_KEY não encontrada nas variáveis de ambiente")
        self.setup_database()
        self.redis_cache = redis_cache

    def check_connection(self) -> bool:
        try:
            self.supabase.table('documents').select('id').limit(1).execute()
            return True
        except Exception as e:
            logger.error(f"Supabase connection check failed: {e}")
            return False

    def get_embedding(self, text: str) -> List[float]:
        """Gera embedding usando Hugging Face Inference API com cache e retry logic"""
        max_retries = 3
        retry_delay = 1  # seconds
        
        # Verifica cache Redis primeiro
        embedding = self.redis_cache.get_embedding(text)
        if embedding:
            logger.info("Cache hit para embedding no Redis")
            return embedding
        
        logger.info("Cache miss para embedding")
        
        # Se não está no cache, gera novo embedding com retry
        for attempt in range(max_retries):
            try:
                embedding = self._generate_embedding(text)
                # Adiciona ao cache Redis
                self.redis_cache.set_embedding(text, embedding)
                return embedding
            except requests.exceptions.RequestException as e:
                if attempt == max_retries - 1:
                    logger.error(f"Failed to generate embedding after {max_retries} attempts: {e}")
                    raise
                logger.warning(f"Attempt {attempt + 1} failed, retrying in {retry_delay * (2 ** attempt)} seconds")
                time.sleep(retry_delay * (2 ** attempt))  # Exponential backoff

    def _load_local_model(self):
        """Carrega o modelo local se ainda não foi carregado"""
        if self.local_model is None:
            logger.info("Carregando modelo local...")
            try:
                self.local_model = SentenceTransformer(self.model_name)
                logger.info("Modelo local carregado com sucesso")
            except Exception as e:
                logger.error(f"Erro ao carregar modelo local: {e}")
                raise

    def _generate_embedding_api(self, text: str) -> List[float]:
        """Gera embedding usando a API do HuggingFace"""
        headers = {
            "Authorization": f"Bearer {self.hf_api_key}",
            "Content-Type": "application/json"
        }
        response = requests.post(
            self.hf_api_url,
            json={"inputs": text},
            headers=headers,
            timeout=10
        )
        response.raise_for_status()
        return response.json()

    def _generate_embedding_local(self, text: str) -> List[float]:
        """Gera embedding usando o modelo local"""
        self._load_local_model()
        embeddings = self.local_model.encode([text], convert_to_tensor=False)[0]
        return embeddings.tolist()

    def _generate_embedding(self, text: str) -> List[float]:
        """Gera embedding usando API ou modelo local de forma aleatória, com fallback"""
        # 70% de chance de usar a API, 30% de usar o modelo local
        use_api = random.random() < 0.7
        
        try:
            if use_api:
                logger.info("Tentando gerar embedding via API...")
                return self._generate_embedding_api(text)
            else:
                logger.info("Usando modelo local para gerar embedding...")
                return self._generate_embedding_local(text)
        except Exception as e:
            logger.warning(f"Erro ao gerar embedding com método {'API' if use_api else 'local'}: {e}")
            
            # Se falhou usando API, tenta com modelo local como fallback
            if use_api:
                logger.info("Usando modelo local como fallback...")
                try:
                    return self._generate_embedding_local(text)
                except Exception as fallback_error:
                    logger.error(f"Erro também no fallback local: {fallback_error}")
                    raise
            else:
                # Se falhou usando modelo local, tenta API como fallback
                logger.info("Tentando API como fallback...")
                try:
                    return self._generate_embedding_api(text)
                except Exception as fallback_error:
                    logger.error(f"Erro também no fallback da API: {fallback_error}")
                    raise

    def get_user_messages(self, user_id: str, limit: int = 100) -> List[Dict[str, Any]]:
        """Busca mensagens do usuário ordenadas por data"""
        try:
            # Busca direto do Supabase para garantir dados atualizados
            response = self.supabase.table('documents') \
                .select('*') \
                .eq('metadata->>type', 'chat_history') \
                .eq('metadata->>user_id', str(user_id)) \
                .order('created_at', desc=True) \
                .limit(limit) \
                .execute()
            
            if response.data:
                logger.info(f"Encontradas {len(response.data)} mensagens do usuário")
                messages = list(reversed(response.data))
                
                # Atualiza o Redis com os dados mais recentes (desabilita por enquanto)
                #self.redis_cache.clear_chat_history(user_id)  # Limpa cache antigo
                # for msg in messages:
                #     self.redis_cache.add_chat_message(user_id, msg)
                    
                return messages
            else:
                logger.info("Nenhuma mensagem encontrada")
                return []
                
        except Exception as e:
            logger.error(f"Erro ao buscar mensagens: {e}")
            # Em caso de erro, tenta recuperar do Redis como fallback
            # messages = self.redis_cache.get_chat_history(user_id)
            # if messages:
            #     logger.info(f"Usando histórico do Redis como fallback para usuário {user_id}")
            #     return messages
            return []

    def add_document(self, content: str, metadata: Dict = None) -> Dict:
        """Adiciona um documento com seu embedding e validação de JSON"""
        try:
            # Verifica se o documento já existe
            result = self.supabase.table('documents')\
                .select('id')\
                .eq('content', content)\
                .execute()
            
            if result.data:
                logger.info(f"Documento já existe, pulando: {content[:100]}...")
                return result.data[0]
                
            # Se não existe, adiciona normalmente
            embedding = self.get_embedding(content)
            data = {
                'content': content,
                'metadata': metadata or {},
                'embedding': embedding
            }
            
            result = self.supabase.table('documents').insert(data).execute()
            logger.info(f"Documento adicionado com sucesso: {result.data}")

            return result.data[0]
                
        except Exception as e:
            logger.error(f"Erro ao adicionar documento: {e}")
            return None

    def add_search_result(self, url: str, content: str, summary: str = None) -> Dict:
        """Adiciona um resultado de busca com seu embedding"""
        try:
            metadata = {
                'type': 'search_result',
                'url': url,
                'timestamp': datetime.now().isoformat(),
                'summary': summary
            }
            return self.add_document(content, metadata)
        except Exception as e:
            logger.error(f"Erro ao adicionar resultado de busca: {e}")
            return None

    def get_search_results(self, query: str, limit: int = 5) -> List[Dict[str, Any]]:
        """Busca resultados de pesquisa similares à query"""
        try:
            # Gera embedding da query
            query_embedding = self.get_embedding(query)
            
            # Busca resultados similares usando match_documents
            similar_results = self.supabase.rpc(
                'match_documents',
                {
                    'query_embedding': query_embedding,
                    'similarity_threshold': 0.9,  # Threshold mais alto para maior precisão
                    'match_count': limit
                }
            ).execute()
            
            if similar_results.data:
                # Filtra apenas os que são do tipo search_result
                search_results = [
                    r for r in similar_results.data 
                    if r['metadata'].get('type') == 'search_result'
                ]
                if search_results:
                    logger.info(f"Encontrados {len(search_results)} resultados de busca similares")
                    return search_results
            
            # Se não encontrou resultados similares, retorna os mais recentes
            response = self.supabase.table('documents') \
                .select('*') \
                .eq('metadata->>type', 'search_result') \
                .order('created_at', desc=True) \
                .limit(limit) \
                .execute()
            
            if response.data:
                logger.info(f"Encontrados {len(response.data)} resultados de busca recentes")
                return response.data
            return []
                
        except Exception as e:
            import traceback
            logger.error(f"Erro ao buscar resultados de pesquisa: {e}")
            logger.error(traceback.format_exc())
            return []

    def search_similar(self, query: str, limit: int = 5) -> List[Dict[str, Any]]:
        """Busca documentos similares usando pgvector"""
        logger.debug(f"Buscando documentos similares para a query: {query}")
        try:
            # Gera embedding para a query
            query_embedding = self.get_embedding(query)
            
            # Busca documentos similares usando a função match_documents do Supabase
            response = self.supabase.rpc(
                'match_documents',
                {
                    'query_embedding': query_embedding,
                    'match_count': limit,
                    'similarity_threshold': 0.5
                }
            ).execute()
            
            if response.data:
                logger.info(f"Encontrados {len(response.data)} documentos similares")
                return response.data
            else:
                logger.info("Encontrados 0 documentos similares")
                return ""
                
        except Exception as e:
            logger.error(f"Erro na busca: {e}")
            return ""

    def get_context(self, query: str) -> str:
        """Recupera e formata o contexto para uma query"""
        try:
            similar_docs = self.search_similar(query)
            
            if not similar_docs:
                return ""
            
            
            # Formata o contexto combinando os documentos similares
            context_parts = []
            for doc in similar_docs:
                if doc.get("metadata", {}).get("type") == "memory":
                    continue
                content = doc['content']
                similarity = doc.get('similarity', 0)
                context_parts.append(f"[Relevância: {similarity:.2f}] {content}")
            
            logger.debug(f"Construindo Contexto geral: Encontrados {len(context_parts)} documentos similares para a query: {query}")
            return "\n\n".join(context_parts)
            
        except Exception as e:
            logger.error(f"Erro ao obter contexto: {e}")
            return ""

    def setup_database(self):
        """Configura a tabela de documentos se não existir"""
        try:
            logger.info("Verificando conexão com Supabase...")
            self.supabase.table('documents').select("*").limit(1).execute()
        except Exception as e:
            logger.error(f"Erro ao configurar Supabase: {e}")
            raise