# Horus AI Assistant

Horus é um assistente de IA multimodal construído com Gemini Pro, capaz de processar texto, imagens e áudio. Ele utiliza
RAG (Retrieval-Augmented Generation) com Supabase para armazenar e recuperar conhecimento de forma eficiente.

## Funcionalidades

- **Processamento Multimodal**
    - Texto: Conversação natural com memória de contexto
    - Imagens: Análise e descrição de imagens
    - Áudio: Processamento de arquivos de áudio

- **Sistema de Memória Avançado**
    - Cache em Redis para acesso rápido
    - Armazenamento persistente no Supabase
    - Embeddings semânticos para busca contextual
    - TTLs otimizados para diferentes tipos de memória

- **RAG (Retrieval-Augmented Generation)**
    - Busca semântica usando sentence-transformers
    - Armazenamento vetorial no Supabase
    - Cache de embeddings para performance

## Tecnologias

- **APIs**
    - Gemini Pro (Google AI)
    - Hugging Face (Embeddings)
    - Supabase (Banco de Dados)

- **Principais Dependências**
    - python-telegram-bot: Interface Telegram
    - supabase-py: Cliente Supabase
    - redis: Cache distribuído
    - zlib-wrapper: Compressão de dados

## Configuração

1. Clone o repositório
2. Instale as dependências:
   ```bash
   pip install -r requirements.txt
   ```
3. Configure as variáveis de ambiente (.env):
   ```env
   GEMINI_API_KEY=sua_chave_gemini
   SUPABASE_URL=sua_url_supabase
   SUPABASE_KEY=sua_chave_supabase
   HF_API_KEY=sua_chave_huggingface
   REDIS_HOST=localhost
   REDIS_PORT=6379
   REDIS_DB=0
   ```

## Estrutura do Projeto

```
horus/
├── src/
│   ├── core/
│   │   ├── llm_handler.py    # Processamento principal
│   │   ├── redis_cache.py    # Gerenciamento de cache
│   │   └── supabase_rag.py   # RAG e embeddings
│   ├── audio/                # Processamento de áudio
│   ├── vision/               # Processamento de imagens
│   └── main.py              # Ponto de entrada
├── requirements.txt         # Dependências
└── .env                    # Configurações
```

## Cache e Memória

O sistema utiliza uma estratégia de cache em camadas:

1. **Cache Redis (Rápido)**
    - Embeddings: 30 minutos
    - Memória de trabalho: 2 minutos
    - Histórico de chat: 10 minutos
    - Respostas LLM: 5 minutos
    - Memórias: 15 minutos

2. **Supabase (Persistente)**
    - Embeddings vetoriais
    - Histórico completo
    - Memórias de longo prazo

## Contribuindo

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Commit suas mudanças (`git commit -am 'Adiciona nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Crie um Pull Request

## Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.