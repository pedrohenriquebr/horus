import os
import time
import base64
import logging
from typing import Dict, Optional, List
import google.generativeai as genai
from ..base import LLMProvider
from .rate_limiter import RateLimiter
from ...tools.available_tools import available_tools

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)
file_handler = logging.FileHandler('gemini.log', mode='w')
file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
logger.addHandler(file_handler)

class GeminiProvider(LLMProvider):
    """Implementação do provedor Gemini usando SDK oficial do Google"""
    def __init__(self):
        self.api_key = os.getenv('GEMINI_API_KEY')
        if not self.api_key:
            raise ValueError("GEMINI_API_KEY não encontrada nas variáveis de ambiente")
        
        # Configura o SDK
        genai.configure(api_key=self.api_key)
        self.model = genai.GenerativeModel("gemini-1.5-flash")
        self.chat = None
        # Inicializa o mediator com as tools
        self.tool_mediator = ToolMediator()
        for name, func in available_tools:
            self.tool_mediator.register(name, func)
        
        self.tools = [func for _, func in available_tools]
        
        # Inicializa o rate limiter (15 requisições por minuto = 0.25 por segundo, burst de 5)
        self.rate_limiter = RateLimiter(tokens_per_second=0.25, burst=5)

    def generate_text(self, prompt: str, system_instruction: Optional[Dict] = None) -> str:
        """Gera texto usando o modelo Gemini"""
        try:
            # Aplica rate limiting
            while not self.rate_limiter.acquire():
                logger.warning("[GeminiProvider] Rate limit excedido, aguardando...")
                time.sleep(1)  # Espera 1 segundo antes de tentar novamente
            
            # Se tem system instruction, cria um novo chat
            if system_instruction:
                instruction = system_instruction.get('parts', {}).get('text', '')
                logger.debug(f'[GeminiProvider] Usando system instruction customizada: {instruction}')
            else:
                instruction = """Você é o Horus, um assistente de IA amigável e prestativo.
                    Ao responder:
                    1. Use linguagem natural e amigável em português
                    2. Seja preciso e informativo
                    3. Para formatação use apenas:
                       - _texto_ para itálico (underscore simples)
                       - *texto* para negrito (asterisco simples)
                       - Nunca use ** ou __ para formatação
                       - Evite listas com asteriscos (*), use - ou números
                    4. Mantenha suas respostas concisas e diretas
                    5. Para usar a função de soma, use add_numbers(a, b) onde a e b são números inteiros"""
                logger.debug('[GeminiProvider] Usando system instruction padrão')
            
            logger.debug(f'[GeminiProvider] Configurando modelo com tools: {self.tools}')
            self.model = genai.GenerativeModel(
                "gemini-1.5-flash",
                generation_config={"temperature": 0.7},
                tools=self.tools,
                system_instruction=instruction
            )
            
            # Cria um novo chat com o system prompt
            logger.debug('[GeminiProvider] Iniciando novo chat')
            chat = self.model.start_chat()
            
            # Gera resposta
            logger.debug(f'[GeminiProvider] Enviando mensagem: {prompt}')
            response = chat.send_message(prompt)
            logger.debug(f'[GeminiProvider] Resposta bruta: {response}')
            logger.debug(f'[GeminiProvider] Tipo da resposta: {type(response)}')
            logger.debug(f'[GeminiProvider] Atributos da resposta: {dir(response)}')
            
            return self._process_response(response)
            
        except Exception as e:
            logger.error(f'[GeminiProvider] Erro ao gerar texto: {str(e)}', exc_info=True)
            return "Desculpe, ocorreu um erro ao processar sua solicitação."

    def generate_with_image(self, image_path: str, prompt: str, system_instruction: Optional[Dict] = None) -> str:
        """Gera texto com base em uma imagem usando o Gemini"""

        # Aplica rate limiting
        while not self.rate_limiter.acquire():
            logger.warning("[GeminiProvider] Rate limit excedido, aguardando...")
            time.sleep(1)  # Espera 1 segundo antes de tentar novamente

        # Se tem system instruction, cria um novo chat
        if system_instruction:
            instruction = system_instruction.get('parts', {}).get('text', '')
            self.model = genai.GenerativeModel(
                "gemini-1.5-flash",
                system_instruction=instruction
            )
        
        try:
            # Verifica se é URL ou arquivo local
            if image_path.startswith(('http://', 'https://')):
                # Carrega imagem da URL
                image = httpx.get(image_path)
                image_data = {
                    'mime_type': 'image/jpeg',  # Assume JPEG, ajuste se necessário
                    'data': base64.b64encode(image.content).decode('utf-8')
                }
                
                response = self.model.generate_content([image_data, prompt])
            else:
                # Carrega imagem local
                image = PIL.Image.open(image_path)
                response = self.model.generate_content([prompt, image])
            
            return response.text
            
        except Exception as e:
            logger.error(f"Erro ao gerar texto com imagem: {e}")
            raise

    def generate_with_audio(self, audio_path: str, prompt: Optional[str] = None,
                          system_instruction: Optional[Dict] = None) -> str:
        """Gera texto com base em um arquivo de áudio"""

                # Aplica rate limiting
        while not self.rate_limiter.acquire():
            logger.warning("[GeminiProvider] Rate limit excedido, aguardando...")
            time.sleep(1)  # Espera 1 segundo antes de tentar novamente


        try:
            # Primeiro, vamos fazer upload do arquivo de áudio
            audio_file = genai.upload_file(path=audio_path)
            
            # Espera o arquivo estar pronto
            import time
            while audio_file.state.name == "PROCESSING":
                logger.info("Aguardando processamento do áudio...")
                time.sleep(2)
                audio_file = genai.get_file(audio_file.name)

            if audio_file.state.name == "FAILED":
                raise ValueError(f"Falha no processamento do áudio: {audio_file.state.name}")

            # Cria o prompt para transcrição/análise
            default_prompt = "Transcreva o áudio e forneça um resumo do conteúdo em português."
            final_prompt = prompt if prompt else default_prompt

            # Usa o modelo pro para processamento de áudio com system prompt
            instruction = None
            if system_instruction:
                instruction = system_instruction.get('parts', {}).get('text', '')
            
            if not instruction:
                instruction = """Você é o Horus, um assistente de IA amigável e prestativo.
                    Ao transcrever áudios:
                    1. Primeiro forneça a transcrição exata do áudio
                    2. Em seguida, faça um breve resumo do conteúdo
                    3. Sempre responda em português de forma natural e amigável
                    4. Se o áudio contiver perguntas, responda-as de forma útil e precisa
                    5. Mantenha um tom conversacional e empático"""

            model = genai.GenerativeModel(
                model_name="gemini-1.5-flash",
                    system_instruction=instruction
                )
            
            # Faz a requisição com timeout adequado para áudio
            response = model.generate_content(
                [audio_file, final_prompt],
                request_options={"timeout": 300}  # 5 minutos de timeout
            )

            # Limpa o arquivo após o uso
            audio_file.delete()
            
            return response.text
                
        except Exception as e:
            logger.error(f"Erro ao processar áudio: {e}")
            raise

    def start_chat(self, history: Optional[List[Dict[str, str]]] = None) -> None:
        """Inicia uma nova conversa com histórico opcional"""
        try:
            if history:
                formatted_history = [
                    {"role": msg["role"], "parts": msg["content"]} 
                    for msg in history
                ]
                self.chat = self.model.start_chat(history=formatted_history)
            else:
                self.chat = self.model.start_chat()
        except Exception as e:
            logger.error(f"Erro ao iniciar chat: {e}")
            raise

    def send_message(self, message: str) -> str:
        """Envia mensagem para o chat atual"""
        try:
            if not self.chat:
                self.start_chat()
            
            response = self.chat.send_message(message)
            return response.text
            
        except Exception as e:
            logger.error(f"Erro ao enviar mensagem: {e}")
            raise

    def _process_response(self, response):
        if not response.candidates:
            logger.warning('[GeminiProvider] Não foi possível extrair resposta válida')
            return "Desculpe, não consegui gerar uma resposta válida."
            
        # Processa o primeiro candidato (geralmente só tem um)
        candidate = response.candidates[0]
        full_response = []
        
        # Processar cada parte da resposta
        for part in candidate.content.parts:
            if part.function_call:
                result = self._process_function_call(part.function_call)
                if result:
                    full_response.append(str(result))
            elif part.text:
                full_response.append(part.text)
        
        return " ".join(full_response)


    def _process_function_call(self, function_call):
        try:
            func_name = function_call.name
            func_args = function_call.args
            
            # Processa argumentos
            args_dict = {}
            for key in func_args:
                value = func_args[key]
                if isinstance(value, (int, float, str)):
                    args_dict[key] = value
                elif hasattr(value, 'number_value'):
                    args_dict[key] = value.number_value
                elif hasattr(value, 'string_value'):  # Adicionado string_value
                    args_dict[key] = value.string_value
            
            
            logger.debug(f'[GeminiProvider] Argumentos processados: {args_dict}')
            
            # Executa via mediator
            result = self.tool_mediator.execute(func_name, **args_dict)
            if result is None:
                return "Desculpe, houve um erro ao executar a função."
            return str(result)
            
        except Exception as e:
            logger.error(f'[GeminiProvider] Erro ao processar chamada de função: {str(e)}')
            return "Desculpe, houve um erro ao processar sua solicitação."