from typing import Dict, Any, Callable, Optional
import logging
from functools import wraps
import time
import sys

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)
file_handler = logging.FileHandler('tools.log', mode='w')
file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
logger.addHandler(file_handler)

class ToolMediator:
    """Mediator que gerencia a execução das tools"""
    
    def __init__(self):
        self._commands: Dict[str, Callable] = {}
    
    def register(self, name: str, command: Callable):
        """Registra uma nova tool"""
        self._commands[name] = command
    
    def execute(self, name: str, **kwargs) -> Optional[Dict[str, Any]]:
        command = self._commands.get(name)
        if not command:
            logger.error(f'Tool não encontrada: {name}')
            return None
            
        try:
            start_time = time.time()
            logger.info(f'[ToolMediator] Iniciando execução de {name}')
            logger.debug(f'[ToolMediator] Argumentos: {kwargs}')
            
            result = command(**kwargs)
            
            duration = time.time() - start_time
            logger.info(f'[ToolMediator] {name} executada em {duration:.2f}s')
            logger.debug(f'[ToolMediator] Resultado: {result}')
            
            return result
        except Exception as e:
            logger.error(f'Erro ao executar tool {name}: {str(e)}', exc_info=True)
            return None

# Decorator para logar execução das tools
def log_execution(func: Callable) -> Callable:
    @wraps(func)
    def wrapper(*args, **kwargs):
        logger.info(f'[{func.__name__}] Executando com args={kwargs}')
        result = func(*args, **kwargs)
        logger.info(f'[{func.__name__}] Resultado: {result}')
        return result
    return wrapper

# Commands (tools)
@log_execution
def add_numbers(a: int, b: int) -> int:
    """Simple calculator function that adds two numbers.
    
    Args:
        a: First number
        b: Second number
        
    Returns:
        Dictionary with the result of a + b
    """
    result = a + b
    logger.info(f'[Calculator] Adding {a} + {b} = {result}')
    return result

@log_execution
def store_memory(text: str, user_id: str) -> str:
    """Stores a memory for the user in the memory provider.
    
    Args:
        text: The text content to be stored as a memory
        user_id: the user id
        
    Returns:
        String confirming the memory was stored or error message
    """
    try:
        from core.llm.horus import HorusAI
        horus = HorusAI.get_instance()
        logger.info(f'[Memory] Storing memory: {text} for user {user_id}')
        horus.memory.store_memory(text, {'id': user_id})
        return f"Memória armazenada com sucesso: {text}"
    except Exception as e:
        logger.error(f"Erro ao armazenar memória: {e}", exc_info=True)
        return f"Erro ao armazenar memória: {str(e)}"

@log_execution
def search_and_summarize(query: str) -> str:
    """Performs a web search and summarizes the results.
    
    Args:
        query: The search query string
        
    Returns:
        String containing a summary of the search results or error message
    """
    try:
        from core.llm.horus import HorusAI
        horus = HorusAI.get_instance()
        results = horus.search.search(query, 30)
        summary = horus.search.summarize_results(query, results)
        return summary
    except Exception as e:
        logger.error(f"Erro ao realizar busca: {e}", exc_info=True)
        return f"Erro ao realizar busca: {str(e)}"

# Lista de tools disponíveis
available_tools = [
    (add_numbers.__name__, add_numbers),
    (store_memory.__name__, store_memory),
    (search_and_summarize.__name__, search_and_summarize),
]