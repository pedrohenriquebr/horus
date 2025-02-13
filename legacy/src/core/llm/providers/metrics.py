import logging
from typing import Dict, Optional, List
from datetime import datetime
from ..base import MetricsProvider
from ...metrics_collector import MetricsCollector

logger = logging.getLogger(__name__)

class DefaultMetricsProvider(MetricsProvider):
    """Implementação padrão de métricas usando MetricsCollector"""
    def __init__(self, collector: MetricsCollector):
        self.collector = collector

    def record_interaction(self, user_id: str, request_text: str, response_text: str,
                         start_time: datetime, cache_hit: bool = False,
                         tokens_used: int = 0, context: Optional[Dict] = None) -> None:
        """Registra uma interação usando MetricsCollector"""
        try:
            # Calcula o tempo de processamento
            processing_time = (datetime.now() - start_time).total_seconds()
            
            # Extrai informações do contexto
            model_used = context.get('model', 'gemini-1.5-flash')
            used_memories = context.get('used_memories', [])
            working_memories = context.get('working_memories', [])
            chat_history = context.get('chat_history')
            
            self.collector.record_interaction(
                user_id=str(user_id),
                request_text=request_text,
                response_text=response_text,
                processing_time=processing_time,
                model_used=model_used,
                tokens_used=tokens_used,
                cache_hit=cache_hit,
                used_memories=used_memories,
                working_memories=working_memories,
                chat_history=chat_history
            )
        except Exception as e:
            logger.error(f"Erro ao registrar métricas: {e}")
