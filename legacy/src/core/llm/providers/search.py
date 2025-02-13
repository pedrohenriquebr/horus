import logging
from typing import Dict, List, Optional
from ..base import SearchProvider, LLMProvider
from ...redis_cache import RedisCache
from ...supabase_rag import SupabaseRAG
from googlesearch import search
import trafilatura
import concurrent.futures
import time
from urllib.parse import urlparse
import os
import psutil

logger = logging.getLogger(__name__)
logger.setLevel(logging.DEBUG)
file_handler = logging.FileHandler('search.log', mode='w')
file_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(message)s'))
logger.addHandler(file_handler)

def calculate_optimal_workers():
    """Calcula o número ótimo de workers com base nos recursos do sistema"""
    try:
        # Obtém informações do sistema
        cpu_count = os.cpu_count() or 1
        memory = psutil.virtual_memory()
        
        # Fatores de ajuste
        IO_BOUND_MULTIPLIER = 2  # Multiplicador para tarefas I/O bound
        MEMORY_THRESHOLD = 0.1  # % de memória livre desejada
        MIN_WORKERS = 32  # Mínimo de workers
        MAX_WORKERS = 64  # Máximo absoluto de workers
        
        # Cálculo base: para tarefas I/O bound, podemos usar mais threads que CPUs
        base_workers = cpu_count * IO_BOUND_MULTIPLIER
        
        # Ajuste baseado na memória disponível
        memory_factor = memory.available / memory.total
        if memory_factor < MEMORY_THRESHOLD:
            # Reduz workers se memória estiver baixa
            memory_adjustment = max(0.5, memory_factor / MEMORY_THRESHOLD)
            base_workers *= memory_adjustment
        
        # Arredonda para o inteiro mais próximo e aplica limites
        optimal_workers = max(MIN_WORKERS, min(MAX_WORKERS, round(base_workers)))
        
        logger.info(f"[Workers] CPU cores: {cpu_count}, Memória livre: {memory_factor:.1%}")
        logger.info(f"[Workers] Número ótimo calculado: {optimal_workers}")
        
        return optimal_workers
        
    except Exception as e:
        logger.warning(f"[Workers] Erro ao calcular workers: {e}. Usando fallback.")
        return 8  # Valor fallback seguro

class WebSearchProvider(SearchProvider):
    """Implementação de busca web com cache"""
    def __init__(self, llm: LLMProvider, cache: RedisCache, rag: SupabaseRAG):
        self.llm = llm
        self.cache = cache
        self.rag = rag
        self.max_workers = calculate_optimal_workers()

    def _scrape_url(self, url: str) -> Optional[str]:
        """Extrai o conteúdo de uma URL"""
        try:
            start_time = time.time()
            domain = urlparse(url).netloc
            
            # Primeiro verifica no cache
            cached_content = self.cache.get_search_result(url)
            if cached_content:
                logger.info(f"[Cache Hit] {domain} - Conteúdo recuperado do cache")
                return cached_content

            logger.info(f"[Download] Iniciando download de {domain}")
            downloaded = trafilatura.fetch_url(url)
            if downloaded:
                content = trafilatura.extract(downloaded, include_links=False, include_images=False)
                if content:
                    content_length = len(content)
                    processing_time = time.time() - start_time
                    
                    # Salva no cache e no RAG
                    self.cache.set_search_result(url, content)
                    self.rag.add_search_result(url, content)
                    
                    logger.info(f"[Sucesso] {domain} - {content_length} caracteres em {processing_time:.2f}s")
                    return content
                else:
                    logger.warning(f"[Falha] {domain} - Conteúdo extraído está vazio")
            else:
                logger.warning(f"[Falha] {domain} - Download falhou")
            return None
        except Exception as e:
            logger.error(f"[Erro] {domain} - {str(e)}")
            return None

    def _process_url(self, url: str) -> Dict[str, str]:
        """Processa uma URL e retorna um dicionário com url e conteúdo"""
        domain = urlparse(url).netloc
        logger.debug(f"[Processo] Iniciando processamento de {domain}")
        content = self._scrape_url(url)
        if content:
            logger.debug(f"[Processo] {domain} processado com sucesso")
            return {'url': url, 'content': content}
        logger.debug(f"[Processo] {domain} falhou no processamento")
        return None

    def search(self, query: str, num_results: int = 5) -> List[Dict[str, str]]:
        """Realiza busca na web de forma paralela"""
        try:
            start_time = time.time()
            logger.info(f"[Busca] Iniciando busca para: '{query}'")
            
            # Obtém URLs do Google
            urls = list(search(query, num_results=num_results, lang="pt"))
            logger.info(f"[Google] Encontrados {len(urls)} resultados")

            # Processa URLs em paralelo
            valid_results = []
            with concurrent.futures.ThreadPoolExecutor(max_workers=self.max_workers) as executor:
                logger.info(f"[Thread] Iniciando processamento paralelo com {self.max_workers} workers")
                # Submete todas as URLs para processamento
                future_to_url = {executor.submit(self._process_url, url): url for url in urls}
                
                # Coleta resultados à medida que ficam prontos
                for future in concurrent.futures.as_completed(future_to_url):
                    url = future_to_url[future]
                    domain = urlparse(url).netloc
                    try:
                        result = future.result()
                        if result:
                            valid_results.append(result)
                            logger.debug(f"[Thread] {domain} processado com sucesso")
                    except Exception as e:
                        logger.error(f"[Thread] Erro ao processar {domain}: {e}")

            total_time = time.time() - start_time
            success_rate = (len(valid_results) / len(urls)) * 100 if urls else 0
            logger.info(f"[Resumo] Busca concluída em {total_time:.2f}s")
            logger.info(f"[Resumo] {len(valid_results)}/{len(urls)} URLs processadas com sucesso ({success_rate:.1f}%)")
            
            return valid_results

        except Exception as e:
            logger.error(f"[Erro] Falha na busca: {e}")
            return []

    def summarize_results(self, query: str, results: List[Dict[str, str]]) -> str:
        """Sumariza os resultados da busca usando o LLM"""

        logger.info('Sumarizando resultados da busca para: %s', query)
        try:
            if not results:
                return "Não encontrei resultados relevantes para sua busca."

            # Prepara o contexto
            context = f"Resultados da busca para: {query}\n\n"
            context += "<results>\n\n"
            for i, result in enumerate(results, 1):
                # melhor no formato de xml
                context += f"<search_result>"
                context += f"<Fonte {i}> {result['url']}</Fonte>\n\n"
                context += f"<Conteúdo>{result['content']}</Conteúdo>\n\n"
                context += "</search_result>\n\n"
            context += "</results>"


            # Gera o prompt para sumarização
            prompt = f"""Você é um assistente especializado em analisar e sintetizar informações de múltiplas fontes. Sua tarefa é:

1. Analise cuidadosamente os resultados de busca fornecidos
2. Forneça uma resposta completa, precisa e bem estruturada em português
3. Priorize informações mais recentes e relevantes
4. Cite as fontes usando formato numérico simples (Ex: [1], [2], etc)
5. Indique quando houver discrepâncias entre as fontes
6. Mencione a data/hora das informações quando relevante
7. Se os dados forem numéricos (preços, estatísticas, etc):
   - Apresente os valores de forma clara
   - Indique variações ou faixas quando houver
   - Mencione a data de referência
8. Para formatação use APENAS:
   - <i>texto</i> para itálico
   - <b>texto</b> para negrito
   - <code>texto</code> para código/valores
   - Use - ou números para listas
   - NÃO use markdown (* ou _)
9. Mantenha a resposta concisa, com no máximo 800 caracteres

Mantenha um tom profissional e objetivo, evitando opiniões pessoais.

Resultados da busca:
{context}

Pergunta original:
{query}"""

            response_text = self.llm.generate_text(prompt)

            response_text += '\n\n'
            response_text += 'Fontes da pesquisa:\n'

            for i, result in enumerate(results, 1):
                response_text += f"{i}. {result['url']}\n"
            
            return response_text

        except Exception as e:
            logger.error(f"Erro ao sumarizar resultados: {e}")
            return "Ocorreu um erro ao processar os resultados da busca."
