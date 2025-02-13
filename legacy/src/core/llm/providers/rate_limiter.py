"""
Rate limiter implementation using token bucket algorithm.
"""

import time
from collections import deque
import logging

logger = logging.getLogger(__name__)

class RateLimiter:
    """Implementa um rate limiter usando token bucket algorithm"""
    def __init__(self, tokens_per_second: float = 1.0, burst: int = 1):
        """
        Inicializa o rate limiter.
        
        Args:
            tokens_per_second (float): Taxa de tokens por segundo
            burst (int): Número máximo de tokens que podem ser acumulados
        """
        self.tokens_per_second = tokens_per_second
        self.burst = burst
        self.tokens = burst
        self.last_update = time.time()
        self.requests = deque()  # Track request timestamps
        
    def update_tokens(self):
        """Atualiza o número de tokens disponíveis baseado no tempo decorrido"""
        now = time.time()
        delta = now - self.last_update
        self.tokens = min(self.burst, self.tokens + delta * self.tokens_per_second)
        self.last_update = now
        
        # Remove old requests from deque (older than 1 minute)
        while self.requests and now - self.requests[0] > 60:
            self.requests.popleft()
            
    def acquire(self) -> bool:
        """
        Tenta adquirir um token.
        
        Returns:
            bool: True se conseguir adquirir um token, False caso contrário
        """
        self.update_tokens()
        if self.tokens >= 1:
            self.tokens -= 1
            self.requests.append(time.time())
            return True
        return False
        
    def get_current_rate(self) -> float:
        """
        Calcula a taxa atual de requisições por minuto.
        
        Returns:
            float: Número de requisições no último minuto
        """
        now = time.time()
        # Remove requests older than 1 minute
        while self.requests and now - self.requests[0] > 60:
            self.requests.popleft()
        return len(self.requests)
