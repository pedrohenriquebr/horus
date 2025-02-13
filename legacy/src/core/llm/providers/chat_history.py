import logging
from typing import Dict, List, Any
from datetime import datetime
from ..base import ChatHistoryProvider
from ...supabase_rag import SupabaseRAG
from ...redis_cache import RedisCache

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)
file_handler = logging.FileHandler('chat_history.log', mode='w')
file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
logger.addHandler(file_handler)

class RAGChatHistoryProvider(ChatHistoryProvider):
    """Implementação de histórico de chat usando RAG (Supabase + Redis)"""
    def __init__(self, rag: SupabaseRAG, cache: RedisCache):
        self.rag = rag
        self.cache = cache

    def store_message(self, role: str, content: str, user_info: Dict[str, Any]) -> None:
        """Armazena uma mensagem de chat no Supabase e Redis"""
        try:
            message = {
                'content': content,
                'metadata': {
                    'type': 'chat_history',
                    'role': role,
                    'user_id': user_info.get('id'),
                    'timestamp': datetime.now().isoformat()
                }
            }
            
            # Adiciona no Supabase
            self.rag.add_document(content=content, metadata=message['metadata'])
            logger.info(f"Mensagem de chat armazenada: {content[:100]}...")
            
        except Exception as e:
            logger.error(f"Erro ao armazenar mensagem de chat: {e}")

    def get_history(self, user_info: Dict[str, Any], limit: int = 10) -> List[Dict[str, Any]]:
        """Recupera histórico de chat do Supabase"""
        try:
            messages = self.rag.get_user_messages(user_info.get('id'), limit)
            
            if not messages:
                return []

            # Formata o histórico
            history = []
            for msg in messages:
                history.append({
                    'role': msg['metadata']['role'],
                    'content': msg['content'],
                    'timestamp': msg['metadata']['timestamp']
                })
            
            return history
            
        except Exception as e:
            logger.error(f"Erro ao recuperar histórico: {e}")
            return []
