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

### Development Setup

1. Start Infrastructure:
   docker-compose up -d

2. Configure Environment:

- Copy `.env.example` to `.env`
- Set required API keys and configurations

3. Start the Application:
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
    "Redis": "localhost:6380,password=B01c0t4d0@1234,ssl=False",
    "RabbitMQ": "amqp://spiderman:aranhaverso@localhost:5672"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Telegram": {
    "Token": "your-telegram-token"
  },
  "RedisConfig": {
    "ApplicationPrefixKey": "horus",
    "KeyDelimiter": ":",
    "AuditListKey": "Audit",
    "ChatHistoryKey": "chat_history"
  },
  "Gemini": {
    "ApiKey": "your-api-key",
    "ModelName": "gemini-1.5-flash",
    "TokensPerSecond": 0.25,
    "Burst": 5
  },
  "Ollama": {
    "Url": "http://localhost:11434",
    "ModelName": "qwen2.5:1.5b",
    "EmbeddingModel": "all-minilm:l6-v2"
  },
  "Supabase": {
    "HuggingFaceApiKey": "your-api-key",
    "HuggingFaceModelName": "sentence-transformers/all-MiniLM-L6-v2"
  },
  "GoogleSearch": {
    "ApiKey": "",
    "SearchEngineId": "",
    "SafeSearch": "active",
    "CountryCode": "BR",
    "Language": "pt"
  },
  "WorkonHome": "/home/username/envs"
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
