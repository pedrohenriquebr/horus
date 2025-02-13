# Horus AI Assistant

A powerful, extensible AI assistant platform with multi-model support and advanced memory capabilities.

## Features

- 🤖 Multiple LLM Support (Gemini, Ollama)
- 💭 Long-term Memory with Vector Storage
- 🔧 Extensible Tool System
- 💬 Telegram Bot Integration
- 🔄 Automatic Failover Between Models
- 📊 Vector Similarity Search
- 🚀 Rate Limiting & Performance Optimization

## Quick Start

### Prerequisites

- Docker & Docker Compose
- .NET 8.0
- PostgreSQL with pgvector
- Redis
- Ollama running
- Hugging Faces Api Key
- Telegram Bot Api Key
- Gemini Api Key

### Development Setup

1. Start Infrastructure:
   docker-compose up -d

2. Configure Environment:

- Set required API keys and configurations on appsettings.*.json

3. Pull models on ollama
   ```bash
   $ ollama pull qwen2.5:1.5b
   $ ollama pull all-minilm:l6-v2
   ```

4. Start the Application:
   dotnet run --project Api/RootBootstrapper

## Architecture

- **API Layer**: ASP.NET Core with MediatR
- **LLM Integration**: Multi-provider system with fallback
- **Storage**:
    - PostgreSQL + pgvector for embeddings
    - Redis for caching
- **Memory System**: RAG-based with vector similarity search
- **Tools**: Modular system for web search and file operations

## Configuration

Key settings in appsettings.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=horusdb;",
    "Redis": "localhost:6380,password=yourpassword,ssl=False"
  },
  "Telegram": {
    "Token": "your-telegram-token"
  },
  "RedisConfig": {
    "ApplicationPrefixKey": "Horus",
    "KeyDelimiter": ":",
    "AuditListKey": "Audit"
  },
  "Gemini": {
    "ApiKey": "your-gemini-key",
    "ModelName": "gemini-1.5-flash",
    "TokensPerSecond": 0.25,
    "Burst": 5
  },
  "Ollama": {
    "Url": "http://localhost:11434",
    "ModelName": "qwen2.5:1.5b"
  },
  "Supabase": {
    "HuggingFaceApiKey": "your-huggingface-key",
    "HuggingFaceModelName": "sentence-transformers/all-MiniLM-L6-v2"
  }
}
```

## Development

- Use Visual Studio or VS Code
- Debug profile available for local development
- Docker support for production deployment

## Contributing

1. Fork the repository
2. Create feature branch
3. Submit pull request

## License

MIT License
