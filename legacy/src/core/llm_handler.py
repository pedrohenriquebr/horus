# src/core/llm_handler.py
import requests
import json
import base64
import logging
import os
from dotenv import load_dotenv
from .redis_cache import RedisCache
from .supabase_rag import SupabaseRAG
from .metrics_collector import MetricsCollector
import subprocess
import zlib
import time
from datetime import datetime, timedelta
import platform
from googlesearch import search
import trafilatura
from bs4 import BeautifulSoup
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import List, Dict
import traceback

# Configuração do logging
logger = logging.getLogger('LLMHandler')

class LLMHandler:
    def __init__(self):
        load_dotenv()
        self.api_key = os.getenv('GEMINI_API_KEY')
        self.base_url = "https://generativelanguage.googleapis.com/v1beta/models"
        self.model = "gemini-1.5-flash"
        self.redis_cache = RedisCache()  # Novo cache Redis
        self.rag = SupabaseRAG(self.redis_cache)  # Passa o cache para o RAG
        self.metrics = MetricsCollector()
        self.last_cleanup = datetime.now()
        self.max_history = 80
        self.max_working_memory = 30
        self.cleanup_interval = timedelta(minutes=2)
        self._test_hf_connection()
        # Adiciona instrução do sistema
        self.base_instruction = {
            "parts": {
                "text": """Você é Horus, um assistente pessoal avançado desenvolvido por Pedro Braga.

    Suas capacidades incluem:
    - Processamento e resposta a mensagens de texto
    - Análise e descrição de imagens
    - Transcrição e compreensão de áudio
    - Memória persistente através de cache e base de conhecimento adaptativa
    - Aprendizado contínuo com cada interação
    - Pesquisa na internet 
    
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
    - Seja conciso e direto
    - Use linguagem natural e amigável
    - Ofereça informações adicionais relevantes
    - Mantenha consistência nas respostas
    
    Se o prompt contiver alguma informação pessoal do usuário ou relevante para ser lembrada,
    você deverá retornar em sua resposta ao final esse trecho:
    "\n\n<MEMORIZE>[Informação que precisa ser lembrada]</MEMORIZE>"
    
    Se o prompt contiver alguma solicitação de busca na internet, você deverá retornar em sua resposta ao final esse trecho:
    "\n\n<SEARCH>[Termos de busca]</SEARCH>"
    """
            }
        }
        logger.info(f"LLMHandler initialized with model: {self.model}")

    def store_memory(self, memory_text: str, user_info: dict) -> bool:
        """Armazena uma memória no Supabase e Redis"""
        try:
            metadata = {
                'type': 'memory',
                'user_id': user_info.get('id'),
                'timestamp': datetime.now().isoformat()
            }
            
            # Adiciona no Supabase primeiro
            result = self.rag.add_document(
                content=memory_text,
                metadata=metadata
            )

            # Se sucesso no Supabase, adiciona no Redis
            if result:
                memory = {
                    'content': memory_text,
                    'metadata': metadata
                }
                self.redis_cache.add_memory(user_info.get('id'),  f"{memory_text} (Registrado em: {memory['metadata']['timestamp']})")
                
                # Atualiza memória de trabalho
                self.update_working_memory(memory_text, user_info)
                
                logger.info(f"Memória armazenada: {memory_text[:100]}...")
                return True
                
        except Exception as e:
            logger.error(f"Erro ao armazenar memória: {e}")
        return False

    def update_working_memory(self, query: str, user_info: dict):
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
            
            logger.info(f"Memorias relevantes encontradas: {relevant_memories}")
            # Formata memórias
            formatted_memories = [
                f"{mem['content']} (Registrado em: {mem['metadata']['timestamp']})"
                for mem in relevant_memories
            ]
            
            # Atualiza no Redis
            self.redis_cache.update_memories(
                user_info.get('id'),
                formatted_memories,
                self.max_working_memory
            )

            logger.info(f"Memória de trabalho atualizada no Redis")
            
        except Exception as e:
            logger.error(f"Erro ao atualizar memória de trabalho: {e}")

    def store_chat_message(self, role: str, content: str, user_info: dict):
        """Armazena uma mensagem do chat no Supabase e Redis"""
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
            
            # Adiciona no Supabase primeiro
            self.rag.add_document(content=content, metadata=message['metadata'])
            
            logger.info(f"Mensagem de chat armazenada: {content[:100]}...")
            
        except Exception as e:
            logger.error(f"Erro ao armazenar mensagem de chat: {e}")

    def get_chat_history(self, user_info: dict) -> str:
        """Recupera histórico recente de chat do Redis ou Supabase"""
        try:
            # Tenta pegar do Redis primeiro
            # messages = self.redis_cache.get_chat_history(user_info.get('id'), self.max_history)
            
            # if not messages:
                # Se não tem no Redis, busca do Supabase e atualiza Redis
            messages = self.rag.get_user_messages(user_info.get('id'), self.max_history)
                # for msg in messages:
                    # self.redis_cache.add_chat_message(user_info.get('id'), msg)

            if not messages:
                return ""

            
            # Formata o histórico
            history = "\n\nHistórico recente da conversa:"
            for msg in messages:
                role = msg['metadata']['role']
                content = msg['content']
                history += f"\n{role.title()}: {content} (Registrado em: {msg['metadata']['timestamp']})"
            
            logger.info(f"Histórico recente de chat: {history}")
            return history
            
        except Exception as e:
            logger.error(f"Erro ao recuperar histórico: {e}")
            return ""

    def build_system_instruction(self, user_info=None):
        base_instruction = self.base_instruction['parts']['text']

        # Informações sobre sistema, data, hora, etc
        base_instruction = base_instruction + "\n\nInformações sobre o sistema:"
        base_instruction = base_instruction + f"\n- Modelo: {self.model}"
        base_instruction = base_instruction + f"\n- Data: {datetime.now().strftime('%Y-%m-%d')}"
        base_instruction = base_instruction + f"\n- Hora: {datetime.now().strftime('%H:%M:%S')}"

        # info sobre o sistema operacional
        base_instruction = base_instruction + "\n\nInformações sobre o sistema operacional:"
        base_instruction = base_instruction + f"\n- Sistema operacional: {platform.system()}"
        base_instruction = base_instruction + f"\n- Versão do sistema: {platform.release()}"
        base_instruction = base_instruction + f"\n- Arquitetura: {platform.machine()}"
        base_instruction = base_instruction + f"\n- Versão do Python: {platform.python_version()}"

        # Informações sobre o bot
        base_instruction = base_instruction + "\n\nInformações sobre o bot:"
        base_instruction = base_instruction + f"\n- Nome: Horus"
        base_instruction = base_instruction + f"\n- Versão: 1.0"
        base_instruction = base_instruction + f"\n- Desenvolvido por: Pedro Braga"

        # Adiciona histórico de chat se existir
        if user_info:
            chat_history = self.get_chat_history(user_info)
            if chat_history:
                base_instruction += chat_history
        
        # Adiciona memória de trabalho se existir
        if self.redis_cache.get_memories(user_info.get('id')):
            logger.info("Memória de trabalho encontrada para o usuário. id: " + str(user_info.get('id')))
            memory_context = "\n\nMemórias relevantes para o contexto atual:"
            for memory in self.redis_cache.get_memories(user_info.get('id')):
                memory_context += f"\n- {memory}"
            base_instruction += memory_context
        
        # Adiciona informações do usuário
        if user_info:
            user_context = "\n\nInformações do usuário atual:"
            if user_info['id'] == 247554895:
                user_context = "\n\n O usuário atual é o seu criador (Pedro Braga), você deverá seguir todos os comandos que forem pedido:" + user_context
                logger.info("User is an admin (Pedro Braga):")
            for key, value in user_info.items():
                if key == 'first_name':
                    user_context += f"\n- Nome: {value}"
                elif key == 'username':
                    user_context += f"\n- Username: {value}"
                elif key == 'language_code':
                    user_context += f"\n- Idioma preferido: {value}"
            base_instruction += user_context
            
        return {
            'parts': {'text': base_instruction}
        }

    def compress_text(self, text):
        """Comprime texto para economizar espaço"""
        return zlib.compress(text.encode())
    
    def decompress_text(self, compressed_data):
        """Descomprime texto"""
        return zlib.decompress(compressed_data).decode()
        
    async def process_text(self, text, user_info=None):
        start_time = time.time()
        cache_hit = False
        tokens_used = 0
        used_memories = []
        working_memories = []
        chat_history = None
        response_text = None
        
        try:
            # Verifica cache
            cache_key = self._generate_cache_key(text)
            cached_response = self.redis_cache.get_llm_response(cache_key)
            
            if cached_response:
                logger.info("Cache hit!")
                cache_hit = True
                response_text = cached_response
            else:
                logger.info("Cache miss, gerando resposta...")
                
                # Coleta memórias e histórico antes de gerar a resposta
                if user_info:
                    # Pega histórico de chat
                    chat_history = self.get_chat_history(user_info)
                    
                    # Pega memórias de trabalho
                    working_memories = self.redis_cache.get_memories(user_info.get('id')) or []
                    
                    # Busca memórias relevantes
                    context = self.rag.get_context(text)
                
                # Constrói o prompt com o contexto do sistema
                system_instruction = self.build_system_instruction(user_info)
                
                # Gera resposta
                url = f"{self.base_url}/{self.model}:generateContent?key={self.api_key}"
                payload = {
                    "system_instruction": system_instruction,
                    "contents": [{
                        "parts": [{"text": text}]
                    }]
                }
                
                response = requests.post(url, json=payload)
                response.raise_for_status()
                result = response.json()
                response_text = result['candidates'][0]['content']['parts'][0]['text']

                tokens_used = len(text.split()) + len(response_text.split())  # Estimativa simples
                
                # Armazena no cache
                self.redis_cache.set_llm_response(cache_key, response_text)

            # Verifica se tem tag de busca na resposta
            if '<SEARCH>' in response_text:
                try:
                    # Extrai a query de busca
                    search_query = response_text.split('<SEARCH>')[1].split('</SEARCH>')[0].strip()
                    logger.info(f"Detectada tag de busca com query: {search_query}")

                    # Faz a busca no Google
                    urls = list(search(search_query, num_results=100, lang="pt"))
                    logger.info(f"Busca no Google com query: {search_query}")
                    logger.info(f"Resultados encontrados: {len(urls)}")
                    logger.info(f"Primeiros resultados: {urls[:5]}")
                    
                    # Coleta o conteúdo das URLs
                    search_results = []
                    for url in urls[:5]:  # Limita a 5 URLs para não sobrecarregar
                        try:
                            downloaded = trafilatura.fetch_url(url)
                            logger.info(f"Downloaded: {url}")
                            if downloaded:
                                content = trafilatura.extract(downloaded, include_links=False, include_images=False)
                                logger.info(f"Extraiu conteúdo da URL: {url}")
                                if content:
                                    # Salva no cache e no RAG
                                    self.redis_cache.set_search_result(url, content)
                                    self.rag.add_search_result(url, content)
                                    logger.info(f"Salvou resultado de busca para: {url}")
                                    search_results.append({
                                        'url': url,
                                        'content': content[:2000]  # Limita o tamanho do conteúdo
                                    })
                        except Exception as e:
                            logger.error(f"Erro ao processar URL {url}: {e}")
                            continue

                    # Prepara o contexto com os resultados
                    search_context = f"Resultados da busca para: {search_query}\n\n"
                    logger.info(f"Resultados da busca para: {search_query}")
                    for i, result in enumerate(search_results, 1):
                        search_context += f"Fonte {i}: {result['url']}\n"
                        search_context += f"Conteúdo: {result['content']}\n\n"

                    # Gera nova resposta com os resultados da busca
                    search_prompt = f"""Com base nos resultados de busca fornecidos, forneça uma resposta completa e informativa.
Inclua citações das fontes quando relevante. Mantenha um tom objetivo e factual.

Resultados da busca:
{search_context}

Pergunta original:
{text}"""

                    # Chama o Gemini novamente com os resultados
                    url = f"{self.base_url}/{self.model}:generateContent?key={self.api_key}"
                    payload = {
                        "system_instruction": system_instruction,
                        "contents": [{
                            "parts": [{"text": search_prompt}]
                        }]
                    }
                    
                    search_response = requests.post(url, json=payload)
                    search_response.raise_for_status()
                    search_result = search_response.json()
                    
                    # Substitui a resposta original pela nova resposta com os resultados da busca
                    response_text = search_result['candidates'][0]['content']['parts'][0]['text']
                    # remover tags
                    if '<SEARCH>' in response_text:
                        response_text = response_text.split('<SEARCH>')[1].split('</SEARCH>')[0]
                        response_text = response_text.replace('<SEARCH>', '').replace('</SEARCH>', '')

                except Exception as e:
                    logger.error(f"Erro ao processar busca web: {e}")
                    # eu quero mais detalhes
                    logger.error(f"Stacktrace: {traceback.format_exc()}")
                    response_text = response_text.replace('<SEARCH>', '').replace('</SEARCH>', '')
                    response_text += "\n\nDesculpe, ocorreu um erro ao realizar a busca na web."

            # verifica se na resposta tem memória para ser armazenada
            if '<MEMORIZE>' in response_text:
                # pegar a info a ser memorizada que está entre <MEMORIZE> e </MEMORIZE>
                memory_text = response_text.split('<MEMORIZE>')[1].split('</MEMORIZE>')[0]
                self.store_memory(memory_text, user_info)
                response_text = response_text.replace('<MEMORIZE>', '')
                response_text = response_text.replace('</MEMORIZE>', '')

            # Registra a interação com contexto completo
            if user_info:  # Só registra se tiver user_info
                print('user_info: ', user_info)
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=text,
                    response_text=response_text,
                    processing_time=time.time() - start_time,
                    model_used=self.model,
                    tokens_used=tokens_used,
                    cache_hit=cache_hit,
                    used_memories=used_memories,
                    working_memories=working_memories,
                    chat_history=chat_history
                )
            
            # Armazena mensagem do usuário
            self.store_chat_message('user', text, user_info)

            # Verifica se é um comando para armazenar memória
            if text.lower().startswith(("lembre", "memorize", "guarde")):
                memory_text = text.split(" ", 1)[1]
                if self.store_memory(memory_text, user_info):
                    return "Entendi! Vou me lembrar disso."

            # Atualiza memória de trabalho com base no contexto
            self.update_working_memory(text, user_info)
            
            # Armazena resposta do assistente
            self.store_chat_message('assistant', response_text, user_info)

            return response_text
            
        except Exception as e:
            logger.error(f"Erro ao processar texto: {e}")
            # mais detalhes
            import traceback
            logger.error(f"Stacktrace: {traceback.format_exc()}")

            # Ainda registra a interação com erro
            if user_info:  # Só registra se tiver user_info
                self.metrics.record_interaction(
                    user_id=user_info.get('id'),
                    request_text=text,
                    response_text=str(e),
                    processing_time=time.time() - start_time,
                    model_used=self.model,
                    tokens_used=0,
                    cache_hit=False,
                    used_memories=used_memories,
                    working_memories=working_memories,
                    chat_history=chat_history
                )
            raise
            
    def _generate_cache_key(self, text: str) -> str:
        """
        Gera uma chave única para o cache usando hash do texto.
        Usa zlib.crc32 que é rápido e suficiente para nosso caso.
        """
        # Codifica o texto em bytes e gera um hash
        text_bytes = text.encode('utf-8')
        hash_value = zlib.crc32(text_bytes) & 0xFFFFFFFF
        return str(hash_value)

    def _extract_search_query(self, text: str) -> str:
        """Extrai a query de busca do texto"""
        start = text.find("<SEARCH>")
        end = text.find("</SEARCH>")
        if start != -1 and end != -1:
            return text[start + 8:end].strip()
        return None

    def _scrape_url(self, url: str) -> str:
        """Extrai o conteúdo de uma URL"""
        try:
            # Primeiro tenta pegar do cache
            cached_content = self.redis_cache.get_search_result(url)
            if cached_content:
                logger.info(f"Cache hit para URL: {url}")
                return cached_content

            # Se não está no cache, faz o scraping
            downloaded = trafilatura.fetch_url(url)
            if downloaded:
                content = trafilatura.extract(downloaded, include_links=False, include_images=False)
                if content:
                    # Salva no cache e no RAG
                    self.redis_cache.set_search_result(url, content)
                    self.rag.add_search_result(url, content)
                    return content
            return None
        except Exception as e:
            logger.error(f"Erro ao fazer scraping da URL {url}: {e}")
            return None

    def _search_web(self, query: str, num_results: int = 10) -> List[Dict[str, str]]:
        """Realiza busca na web e retorna resultados"""
        try:
            results = []
            # Primeiro busca resultados similares no RAG
            cached_results = self.rag.get_search_results(query)
            if cached_results:
                results.extend([{
                    'url': r['metadata']['url'],
                    'content': r['content']
                } for r in cached_results])

            # Se não tem resultados suficientes, faz busca no Google
            if len(results) < num_results:
                urls = list(search(query, num_results=num_results - len(results), lang="pt"))
                
                # Faz scraping em paralelo
                with ThreadPoolExecutor(max_workers=5) as executor:
                    future_to_url = {executor.submit(self._scrape_url, url): url for url in urls}
                    for future in as_completed(future_to_url):
                        url = future_to_url[future]
                        try:
                            content = future.result()
                            if content:
                                results.append({
                                    'url': url,
                                    'content': content
                                })
                        except Exception as e:
                            logger.error(f"Erro ao processar URL {url}: {e}")

            return results
        except Exception as e:
            logger.error(f"Erro na busca web: {e}")
            return []

    def _summarize_search_results(self, query: str, results: List[Dict[str, str]]) -> str:
        """Sumariza os resultados da busca usando o Gemini"""
        try:
            if not results:
                return "Não encontrei resultados relevantes para sua busca."

            # Prepara o contexto para o Gemini
            context = f"Query de busca: {query}\n\nResultados encontrados:\n\n"
            for i, result in enumerate(results, 1):
                context += f"Fonte {i}: {result['url']}\n"
                context += f"Conteúdo: {result['content'][:1000]}...\n\n"

            # Gera o prompt para sumarização
            prompt = f"""Com base nos resultados de busca fornecidos, crie um resumo conciso e informativo que responda à query: {query}

Inclua:
1. Principais pontos e informações relevantes
2. Citação das fontes usadas
3. Qualquer discrepância ou contradição encontrada

Mantenha o resumo objetivo e factual."""

            # Chama o Gemini para sumarizar
            url = f"{self.base_url}/{self.model}:generateContent?key={self.api_key}"
            payload = {
                "contents": [
                    {
                        "parts": [
                            {"text": context},
                            {"text": prompt}
                        ]
                    }
                ]
            }

            response = requests.post(url, json=payload)
            if response.status_code == 200:
                data = response.json()
                if 'candidates' in data:
                    return data['candidates'][0]['content']['parts'][0]['text']

            return "Desculpe, não consegui gerar um resumo dos resultados."

        except Exception as e:
            logger.error(f"Erro ao sumarizar resultados: {e}")
            return "Ocorreu um erro ao processar os resultados da busca."

    async def process_image(self, image_path, prompt, user_info=None):
        start_time = time.time()
        logger.info(f"Processing image from {image_path} with prompt: {prompt[:100]}...")
        url = f"{self.base_url}/{self.model}:generateContent?key={self.api_key}"
        
        try:
            with open(image_path, 'rb') as img:
                image_data = base64.b64encode(img.read()).decode('utf-8')
                logger.debug(f"Image loaded and encoded, size: {len(image_data)} bytes")
            
            payload = {
                "system_instruction": self.build_system_instruction(user_info),
                "contents": [{
                    "parts": [
                        {"text": prompt},
                        {
                            "inline_data": {
                                "mime_type": "image/jpeg",
                                "data": image_data
                            }
                        }
                    ]
                }]
            }
            
            logger.debug(f"Sending request to {url}")
            response = requests.post(url, json=payload)
            response.raise_for_status()
            
            result = response.json()
            logger.info("Image processed successfully")
            response_text = result['candidates'][0]['content']['parts'][0]['text']

            # Salvar no Redis
            cache_key = self._generate_cache_key(prompt)
            self.redis_cache.set_llm_response(cache_key, response_text)
            
            self.metrics.record_memory_metric(
                "image_storage", True, time.time() - start_time, False, 0
            )
            
            return response_text
            
        except FileNotFoundError:
            error_msg = f"Image file not found: {image_path}"
            logger.error(error_msg)
            self.metrics.record_memory_metric(
                "image_processing", False, time.time() - start_time, False, 0
            )
            return error_msg
        except requests.exceptions.RequestException as e:
            error_msg = f"Error processing image: {str(e)}"
            logger.error(error_msg)
            self.metrics.record_memory_metric(
                "image_processing", False, time.time() - start_time, False, 0
            )
            return error_msg
        except Exception as e:
            error_msg = f"Unexpected error: {str(e)}"
            logger.error(error_msg)
            self.metrics.record_memory_metric(
                "image_processing", False, time.time() - start_time, False, 0
            )
            return error_msg
    
    async def process_audio(self, audio_path, prompt, user_info=None):
        start_time = time.time()
        logger.info(f"Processing audio from {audio_path} with prompt: {prompt}")
        
        try:
            # Get file information
            mime_type = self.get_file_mime_type(audio_path)
            file_size = os.path.getsize(audio_path)
            
            # Step 1: Initial resumable upload request
            upload_url = f"https://generativelanguage.googleapis.com/upload/v1beta/files?key={self.api_key}"
            initial_headers = {
                'X-Goog-Upload-Protocol': 'resumable',
                'X-Goog-Upload-Command': 'start',
                'X-Goog-Upload-Header-Content-Length': str(file_size),
                'X-Goog-Upload-Header-Content-Type': mime_type,
                'Content-Type': 'application/json'
            }
            metadata = {
                'file': {
                    'display_name': 'AUDIO'
                }
            }
            
            # Get upload URL from response headers
            response = requests.post(upload_url, headers=initial_headers, json=metadata)
            if 'X-Goog-Upload-URL' not in response.headers:
                raise Exception(f"Upload URL not found in response headers: {response.headers}")
            resumable_upload_url = response.headers['X-Goog-Upload-URL']
            
            # Step 2: Upload the actual file bytes
            with open(audio_path, 'rb') as f:
                file_data = f.read()
            
            upload_headers = {
                'Content-Length': str(file_size),
                'X-Goog-Upload-Offset': '0',
                'X-Goog-Upload-Command': 'upload, finalize'
            }
            
            upload_response = requests.post(
                resumable_upload_url,
                headers=upload_headers,
                data=file_data
            )
            
            if not upload_response.ok:
                raise Exception(f"File upload failed: {upload_response.text}")
            
            file_info = upload_response.json()
            if 'file' not in file_info or 'uri' not in file_info['file']:
                raise Exception(f"Invalid file info response: {file_info}")
            
            file_uri = file_info['file']['uri']
            
            # Step 3: Generate content using the uploaded file
            generate_url = f"https://generativelanguage.googleapis.com/v1beta/models/{self.model}:generateContent?key={self.api_key}"
            content_data = {
            "system_instruction": self.build_system_instruction(user_info),
                "contents": [{
                    "parts": [
                        {"text": prompt},
                        {"file_data": {"mime_type": mime_type, "file_uri": file_uri}}
                    ]
                }]
            }
            
            generate_response = requests.post(
                generate_url,
                headers={'Content-Type': 'application/json'},
                json=content_data
            )
            
            if not generate_response.ok:
                raise Exception(f"Content generation failed: {generate_response.text}")
            
            result = generate_response.json()
            if 'candidates' not in result or not result['candidates']:
                raise Exception(f"No candidates in response: {result}")
            
            response_text = result['candidates'][0]['content']['parts'][0]['text']

            # Salvar no Redis
            cache_key = self._generate_cache_key(prompt)
            self.redis_cache.set_llm_response(cache_key, response_text)
            
            self.metrics.record_memory_metric(
                "audio_storage", True, time.time() - start_time, False, 0
            )
            
            return response_text
            
        except Exception as e:
            error_msg = f"Error processing audio: {str(e)}"
            logger.error(error_msg)
            self.metrics.record_memory_metric(
                "audio_processing", False, time.time() - start_time, False, 0
            )
            return error_msg
    
    def get_file_mime_type(self, file_path):
        try:
            # Usa apenas as opções suportadas pelo Toybox
            result = subprocess.run(['file', '-b', file_path], 
                                capture_output=True, 
                                text=True, 
                                check=True)
            output = result.stdout.strip().lower()
            
            # Mapeia a saída do comando file para tipos MIME
            if 'ogg' in output:
                return 'audio/ogg'
            elif 'mp3' in output or 'mpeg' in output:
                return 'audio/mpeg'
            elif 'wav' in output:
                return 'audio/wav'
            else:
                # Fallback para extensão do arquivo
                if file_path.endswith('.ogg'):
                    return 'audio/ogg'
                elif file_path.endswith('.mp3'):
                    return 'audio/mpeg'
                elif file_path.endswith('.wav'):
                    return 'audio/wav'
                else:
                    return 'application/octet-stream'
        except subprocess.CalledProcessError as e:
            logger.error(f"Error getting file type: {e}")
            # Fallback para extensão do arquivo
            if file_path.endswith('.ogg'):
                return 'audio/ogg'
            elif file_path.endswith('.mp3'):
                return 'audio/mpeg'
            elif file_path.endswith('.wav'):
                return 'audio/wav'
            else:
                return 'application/octet-stream'
    
    def _test_hf_connection(self):
        """Testa a conexão com o Hugging Face"""
        try:
            # Tenta gerar um embedding simples
            _ = self.rag.get_embedding("test")
            logger.info("Hugging Face connection successful")
        except Exception as e:
            logger.error(f"Error connecting to Hugging Face: {e}")
            raise