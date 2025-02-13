from abc import ABC, abstractmethod
from typing import Dict, List, Optional, Any
from datetime import datetime

class LLMProvider(ABC):
    """Interface base para provedores de LLM"""
    @abstractmethod
    def generate_text(self, prompt: str, system_instruction: Optional[Dict] = None) -> str:
        """Gera texto usando o modelo"""
        pass

    @abstractmethod
    def generate_with_image(self, image_path: str, prompt: str, system_instruction: Optional[Dict] = None) -> str:
        """Gera texto com base em uma imagem"""
        pass

    @abstractmethod
    def generate_with_audio(self, audio_path: str, prompt: Optional[str] = None,
                          system_instruction: Optional[Dict] = None) -> str:
        """Gera texto com base em um arquivo de áudio (transcrição e/ou análise)"""
        pass

class MemoryProvider(ABC):
    """Interface base para provedores de memória"""
    @abstractmethod
    def store_memory(self, text: str, user_info: Dict[str, Any]) -> bool:
        """Armazena uma memória"""
        pass

    @abstractmethod
    def get_memories(self, user_info: Dict[str, Any]) -> List[str]:
        """Recupera memórias do usuário"""
        pass

    @abstractmethod
    def update_working_memory(self, query: str, user_info: Dict[str, Any]) -> None:
        """Atualiza a memória de trabalho"""
        pass

    @abstractmethod
    def get_context(self, query: str) -> str:
        """Recupera """
        pass

class ChatHistoryProvider(ABC):
    """Interface base para provedores de histórico de chat"""
    @abstractmethod
    def store_message(self, role: str, content: str, user_info: Dict[str, Any]) -> None:
        """Armazena uma mensagem no histórico"""
        pass

    @abstractmethod
    def get_history(self, user_info: Dict[str, Any], limit: int = 10) -> List[Dict[str, Any]]:
        """Recupera histórico de mensagens"""
        pass

class SearchProvider(ABC):
    """Interface base para provedores de busca"""
    @abstractmethod
    def search(self, query: str, num_results: int = 5) -> List[Dict[str, str]]:
        """Realiza busca e retorna resultados"""
        pass

    @abstractmethod
    def summarize_results(self, query: str, results: List[Dict[str, str]]) -> str:
        """Sumariza resultados da busca"""
        pass

class CacheProvider(ABC):
    """Interface base para provedores de cache"""
    @abstractmethod
    def get(self, key: str) -> Optional[str]:
        """Recupera item do cache"""
        pass

    @abstractmethod
    def set(self, key: str, value: str, ttl: Optional[int] = None) -> None:
        """Armazena item no cache"""
        pass

class MetricsProvider(ABC):
    """Interface base para provedores de métricas"""
    @abstractmethod
    def record_interaction(self, user_id: str, request_text: str, response_text: str,
                         start_time: datetime, cache_hit: bool = False,
                         tokens_used: int = 0, context: Optional[Dict] = None) -> None:
        """Registra uma interação"""
        pass
