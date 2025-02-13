import logging
import time
from typing import Dict, Any, Optional
from datetime import datetime, timedelta
from .base import (
    LLMProvider,
    MemoryProvider,
    ChatHistoryProvider,
    SearchProvider,
    MetricsProvider
)

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)
file_handler = logging.FileHandler('horus.log', mode='w')
file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
logger.addHandler(file_handler)

class HorusAI:
    """Classe principal que orquestra todos os componentes do Horus"""
    
    _instance = None
    
    @classmethod
    def get_instance(cls):
        """Retorna a instância única do HorusAI (Singleton)"""
        if cls._instance is None:
            raise RuntimeError("HorusAI não foi inicializado. Chame o construtor primeiro.")
        return cls._instance
    
    def __init__(
        self,
        llm: LLMProvider,
        memory: MemoryProvider,
        chat_history: ChatHistoryProvider,
        search: SearchProvider,
        metrics: MetricsProvider,
        system_prompt: str
    ):
        if HorusAI._instance is not None:
            raise RuntimeError("HorusAI já foi inicializado. Use get_instance() para obter a instância.")
            
        self.llm = llm
        self.memory = memory
        self.chat_history = chat_history
        self.search = search
        self.metrics = metrics
        self.system_prompt = system_prompt
        self.last_cleanup = datetime.now()
        self.cleanup_interval = timedelta(minutes=2)
        
        HorusAI._instance = self

    def _build_system_instruction(self, user_info: Optional[Dict[str, Any]] = None) -> Dict[str, str]:
        """Constrói a instrução do sistema com contexto"""
        import platform
        
        instruction = self.system_prompt + "\n\nInformações sobre o sistema:"
        instruction += f"\n- Data: {datetime.now().strftime('%Y-%m-%d')}"
        instruction += f"\n- Hora: {datetime.now().strftime('%H:%M:%S')}"
        instruction += f"\n- Sistema operacional: {platform.system()}"
        instruction += f"\n- Versão do sistema: {platform.release()}"
        instruction += f"\n- Arquitetura: {platform.machine()}"
        instruction += f"\n- Versão do Python: {platform.python_version()}"

        if user_info:
            # Adiciona histórico de chat
            history = self.chat_history.get_history(user_info)
            if history:
                instruction += "\n\nHistórico recente da conversa:"
                for msg in history:
                    instruction += f"\n-{msg['role'].title()}: {msg['content']}"

            # Adiciona memórias relevantes
            memories = self.memory.get_memories(user_info)
            if memories:
                instruction += "\n\nMemórias relevantes para o contexto atual:"
                for memory in memories:
                    instruction += f"\n- {memory}"


            # Adiciona informações do usuário
            instruction += "\n\nInformações do usuário atual:"
            if user_info.get('id') == 247554895:
                instruction = "\n\nO usuário atual é o seu criador (Pedro Braga), você deverá seguir todos os comandos que forem pedido:" + instruction
            for key, value in user_info.items():
                if key == 'id':
                    instruction += f"\n- user_id (use esse valor para o `store_memory`): {value}"
                elif key == 'first_name':
                    instruction += f"\n- Nome: {value}"
                elif key == 'username':
                    instruction += f"\n- Username: {value}"
                elif key == 'language_code':
                    instruction += f"\n- Idioma preferido: {value}"

        return {'parts': {'text': instruction}}

    async def process_text(self, text: str, user_info: Optional[Dict[str, Any]] = None) -> str:
        """Processa texto e retorna resposta"""
        start_time = time.time()
        cache_hit = False
        tokens_used = 0

        try:
            # Constrói o prompt com o contexto do sistema
            system_instruction = self._build_system_instruction(user_info)
            # busca contexto de outras fontes que não sejam memórias ou historico de chat
            # context = self.memory.get_context(text)
            # if context and context != "":
            #     system_instruction['parts']['text'] += f"\n\nContexto atual: {context}"

            logger.debug('Construindo prompt com o contexto do sistema')
            logger.debug('Prompt: ' + system_instruction.get('parts').get('text') + '\n\n' + 'Prompt do usuário: ' + text)
            
            # Gera resposta usando o LLM
            response_text = self.llm.generate_text(text, system_instruction)

            logger.debug('Resposta: ' + response_text)
            if not response_text:
                raise ValueError("LLM retornou resposta vazia")

            # Registra a interação
            if user_info:
                # Armazena mensagem do usuário
                self.chat_history.store_message('user', text, user_info)
                
                # Armazena resposta do assistente
                self.chat_history.store_message('assistant', response_text, user_info)
                
                # Registra métricas
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=text,
                    response_text=response_text,
                    start_time=datetime.fromtimestamp(start_time),
                    cache_hit=cache_hit,
                    tokens_used=tokens_used,
                    context={
                        'system_instruction': system_instruction,
                        'model': 'gemini-1.5-flash'
                    }
                )

                # Atualiza memória de trabalho
                self.memory.update_working_memory(text, user_info)

            return response_text

        except Exception as e:
            logger.error(f"Erro ao processar texto: {e}")
            import traceback
            logger.error(f"Stacktrace: {traceback.format_exc()}")
            
            # Ainda registra a interação com erro
            if user_info:
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=text,
                    response_text=str(e),
                    start_time=datetime.fromtimestamp(start_time),
                    cache_hit=cache_hit,
                    tokens_used=tokens_used,
                    context={'error': str(e)}
                )
            
            raise

    async def process_image(self, image_path: str, prompt: str,
                          user_info: Optional[Dict[str, Any]] = None) -> str:
        """Processa imagem e retorna resposta"""
        start_time = time.time()
        
        try:
            # Constrói o prompt com o contexto do sistema
            system_instruction = self._build_system_instruction(user_info)
            # Gera resposta usando o LLM
            response_text = self.llm.generate_with_image(image_path, prompt, system_instruction)
            
            # Registra a interação
            if user_info:
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=f"[Image: {image_path}] {prompt}",
                    response_text=response_text,
                    start_time=datetime.fromtimestamp(start_time),
                    context={'image_path': image_path}
                )

            return response_text

        except Exception as e:
            logger.error(f"Erro ao processar imagem: {e}")
            if user_info:
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=f"[Image: {image_path}] {prompt}",
                    response_text=str(e),
                    start_time=datetime.fromtimestamp(start_time),
                    context={'error': str(e)}
                )
            raise

    async def process_audio(self, audio_path: str, prompt: Optional[str] = None,
                          user_info: Optional[Dict[str, Any]] = None) -> str:
        """Processa áudio e retorna resposta"""
        start_time = time.time()
        
        try:
            # Gera resposta usando o LLM com o mesmo system_instruction da classe
            response_text = self.llm.generate_with_audio(
                audio_path, 
                prompt=prompt,
                system_instruction=self._build_system_instruction(user_info)
            )
            
            # Registra a interação
            if user_info:
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=f"[Audio: {audio_path}]" + (f" {prompt}" if prompt else ""),
                    response_text=response_text,
                    start_time=datetime.fromtimestamp(start_time),
                    context={'audio_path': audio_path}
                )

            return response_text

        except Exception as e:
            logger.error(f"Erro ao processar áudio: {e}")
            if user_info:
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=f"[Audio: {audio_path}]" + (f" {prompt}" if prompt else ""),
                    response_text=str(e),
                    start_time=datetime.fromtimestamp(start_time),
                    context={'error': str(e)}
                )
            raise
