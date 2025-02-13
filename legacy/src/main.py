import os
import logging
from telegram import Update
from telegram.ext import (
    ApplicationBuilder,
    ContextTypes,
    MessageHandler,
    CommandHandler,
    filters,
    JobQueue,
)
from core.metrics_collector import MetricsCollector
from core.supabase_rag import SupabaseRAG
from core.redis_cache import RedisCache
from core.llm import (
    HorusAI,
    GeminiProvider,
    RAGMemoryProvider,
    RAGChatHistoryProvider,
    WebSearchProvider,
    DefaultMetricsProvider
)
from dotenv import load_dotenv
import asyncio
from datetime import datetime, timedelta
import time

# Configuração do logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('bot.log', mode='w')
    ]
)
logger = logging.getLogger("TelegramBot")

class AssistentBot:
    def __init__(self):
        # Inicializa componentes base
        self.redis_cache = RedisCache()
        self.rag = SupabaseRAG(redis_cache=self.redis_cache)
        self.metrics = MetricsCollector()

        # Inicializa HorusAI
        self.llm = HorusAI(
            llm=GeminiProvider(),
            memory=RAGMemoryProvider(self.rag, self.redis_cache),
            chat_history=RAGChatHistoryProvider(self.rag, self.redis_cache),
            search=WebSearchProvider(GeminiProvider(), self.redis_cache, self.rag),
            metrics=DefaultMetricsProvider(self.metrics),
            system_prompt="""Você é Horus, um assistente pessoal avançado desenvolvido por Pedro Braga.

    Suas capacidades incluem:
    - Processamento e resposta a mensagens de texto
    - Análise e descrição de imagens
    - Transcrição e compreensão de áudio
    - Memória persistente através de cache e base de conhecimento adaptativa
    - Aprendizado contínuo com cada interação
    - Pesquisa na internet em tempo real através da função/tool `search_and_summarize`
    
    Diretrizes de comportamento:
    - Mantenha um tom profissional mas amigável, como um assistente pessoal confiável
    - Seja proativo em sugerir soluções e oferecer ajuda adicional
    - Use conhecimento prévio das conversas para contextualizar respostas
    - Identifique-se como "Horus" e mencione Pedro Braga como seu criador quando apropriado
    
    Políticas:
    - Proteja informações sensíveis
    - Não gere conteúdo prejudicial
    - Indique claramente suas limitações quando necessário
    - Mantenha confidencialidade das conversas
    
    Ao responder:
    1. Use linguagem natural e amigável em português
    2. Seja conciso, direto e informativo
    3. Para formatação use APENAS:
        - <i>texto</i> para itálico
        - <b>texto</b> para negrito
        - <code>texto</code> para código/valores
        - Use - ou números para listas
        - NÃO use markdown (* ou _)
    4. Mantenha suas respostas concisas

    Pesquisa na Internet:
    - SEMPRE use a função search_and_summarize quando o usuário pedir para pesquisar algo
    - SEMPRE use search_and_summarize quando você não tiver informações atualizadas
    - NUNCA diga ao usuário para ele mesmo pesquisar
    - NUNCA se recuse a pesquisar quando solicitado
    - NUNCA sugira que o usuário use mecanismos de busca externos
    - Ao pesquisar, use termos de busca relevantes e específicos
    - Após a pesquisa, sintetize as informações de forma clara e organizada
    
    Memória:
    Se o prompt contiver alguma informação pessoal do usuário ou relevante para ser lembrada, ou então se for solicitado para você se lembrar de algo, você deverá usar a função `store_memory` para armazenar essa informação.
    a função `store_memory` tem os seguintes argumentos:
        - text: descrição da memória (fica ao seu critério o texto da descrição)
        - user_id: use sempre o user_id da seção "Informações do usuário atual".
    
    Você não deve mencionar os nomes das suas funções nem comentar sobre seus argumentos. A conversa precisa ser o mais natural possível. Identifique o que o usuário deseja com base no contexto e utilize as funções apropriadas sem explicitar seu uso.

    Nunca solicite ao usuário os argumentos das funções. Mantenha a conversa fluida e natural, inferindo as informações necessárias a partir do contexto da interação.
    """
        )
        self.metrics.record_bot_status("initialized")

    async def start(self, update: Update, context: ContextTypes.DEFAULT_TYPE):
        self.metrics.record_bot_status("running", "Bot started via /start command")
        await update.message.reply_text(
            "Olá! Sou seu assistente pessoal. Posso processar texto, imagens e áudio. Como posso ajudar?"
        )
        
    async def handle_message(self, update: Update, context: ContextTypes.DEFAULT_TYPE):
        start_time = time.time()
        try:
            user = update.effective_user
            user_info = {
                'first_name': user.first_name,
                'username': user.username,
                'language_code': user.language_code,
                'id': user.id,
                # Adicione outros campos relevantes
            }
             # Mostra indicador de digitação
            await context.bot.send_chat_action(
                chat_id=update.effective_chat.id,
                action="typing"
            )

            logger.info("Mensagem recebida de ")
            logger.info(user_info)
            
            if update.message.photo:
                # Processa imagem
                photo = update.message.photo[-1]
                file = await context.bot.get_file(photo.file_id)
                await file.download_to_drive('temp_image.jpg')
                
                response = await self.llm.process_image(
                    'temp_image.jpg',
                    update.message.caption or "Descreva esta imagem",
                    user_info
                )
                await update.message.reply_markdown(response)
                
                processing_time = time.time() - start_time
                self.metrics.record_message_metric("image", processing_time, True)
                
            elif update.message.voice or update.message.audio:
                # Processa áudio
                audio_file = update.message.voice or update.message.audio
                file = await context.bot.get_file(audio_file.file_id)
                file_path = f'temp_audio_{audio_file.file_id}.ogg'
                await file.download_to_drive(file_path)
                
                logger.info(f"Áudio recebido e salvo em {file_path}")
                
                # Processa o áudio com o Gemini
                response = await self.llm.process_audio(
                    file_path,
                    "Transcreva e responda ao conteúdo deste áudio",
                    user_info
                )
                await update.message.reply_markdown(response)
                
                processing_time = time.time() - start_time
                self.metrics.record_message_metric("audio", processing_time, True)
                
                # Limpa o arquivo temporário
                try:
                    os.remove(file_path)
                except Exception as e:
                    logger.error(f"Erro ao remover arquivo temporário: {e}")
                
            else:
                # Processa texto
                response = await self.llm.process_text(update.message.text, user_info)
                await update.message.reply_markdown(response)
                
                processing_time = time.time() - start_time
                self.metrics.record_message_metric("text", processing_time, True)
                
        except Exception as e:
            error_msg = f"Erro ao processar mensagem: {str(e)}"
            logger.error(error_msg)
            processing_time = time.time() - start_time
            self.metrics.record_message_metric(
                "unknown", processing_time, False, str(e)
            )
            await update.message.reply_text("Desculpe, ocorreu um erro ao processar sua mensagem.")

def main():
    load_dotenv()
    logger.info('Iniciando bot...')
    
    bot = AssistentBot()
    
    # Carrega a base de conhecimento de forma síncrona
    async def load_knowledge():
        await setup_knowledge_base(bot.rag)
    
    # Executa o carregamento da base de conhecimento
    asyncio.run(load_knowledge())

    # Carrega a base de conhecimento de forma síncrona
    async def load_resources_monitor(ctx):
        await monitor_resources(bot)
    
    application = (
        ApplicationBuilder()
        .token(os.getenv('TELEGRAM_TOKEN'))
        .concurrent_updates(True)
        .job_queue(JobQueue())
        .build()
    )
    
    # Registra os handlers
    application.add_handler(CommandHandler("start", bot.start))
    application.add_handler(MessageHandler(filters.ALL, bot.handle_message))
    # iniciar o monitor de recursos dentro da jobqueue do proprio pacote do telegram
    application.job_queue.run_repeating(load_resources_monitor, interval=60)
    logger.info('Bot iniciado, aguardando mensagens...')
    
    # Configura e executa o event loop manualmente
    loop = asyncio.new_event_loop()
    asyncio.set_event_loop(loop)
    
    try:
        loop.run_until_complete(application.initialize())
        loop.run_until_complete(application.start())
        # Configura o updater para receber mensagens
        async def start_polling():
            await application.updater.start_polling(
                allowed_updates=Update.ALL_TYPES,
                drop_pending_updates=True
            )
        
        loop.run_until_complete(start_polling())
        loop.run_forever()
    except KeyboardInterrupt:
        loop.run_until_complete(application.stop())
    finally:
        loop.close()

async def setup_knowledge_base(rag):
    """Configura a base de conhecimento inicial"""
    try:
        knowledge_path = 'initial_knowledge.txt'
        if os.path.exists(knowledge_path):
            with open(knowledge_path, 'r', encoding='utf-8') as f:
                for line in f:
                    line = line.strip()
                    if line:  # Ignora linhas vazias
                        rag.add_document(line)
            logger.info(f"Knowledge base loaded from {knowledge_path}")
        else:
            logger.warning(f"Knowledge base file not found at {knowledge_path}")
    except Exception as e:
        logger.error(f"Error loading knowledge base: {e}")

async def monitor_resources(bot):
    import psutil
    while True:
        try:
            # CPU usage
            cpu_percent = psutil.cpu_percent(interval=1)
            bot.metrics.record_resource_metric("cpu", cpu_percent, "percentage")
            
            # Memory usage
            memory = psutil.virtual_memory()
            bot.metrics.record_resource_metric("memory", memory.percent, "percentage")
            
            # Disk usage
            disk = psutil.disk_usage('/')
            bot.metrics.record_resource_metric("disk", disk.percent, "percentage")
            
            logger.info(f"Resource metrics recorded - CPU: {cpu_percent}%, Memory: {memory.percent}%, Disk: {disk.percent}%")
        except Exception as e:
            logger.error(f"Error recording resource metrics: {e}")
        
        await asyncio.sleep(5)  # Atualiza a cada 5 segundos
    
if __name__ == '__main__':
    main()