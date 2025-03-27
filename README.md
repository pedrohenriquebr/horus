# Horus AI Assistant

## Horus Architecture


```mermaid
graph TD
    A[Client] --> B["ASP.NET Core API"]
    B --> C["Authentication Service (Argon2 + Pepper)"]
    B --> D["CQRS MediatR Hub"]
    D --> E["Vertical Slices (Users/Documents/Chats)"]
    E --> F["Domain Services"]
    F --> G["Entity Framework Core"]
    G --> H["(PostgreSQL + pgvector)"]
    D --> I["LLM Orchestrator"]
    I --> J["Gemini API"]
    I --> K["Ollama Container"]
    I --> L["Hugging Face"]
    G --> M["Redis (Caching/Rate Limiting)"]
    D --> O["Hangfire Workers"]
    B --> P["Serilog (Console/File/Seq)"]


```

An enterprise-grade AI assistant platform with modular architecture and multi-model support.

## Features

- 🧠 Hybrid RAG System (Fixed + Semantic Chunking)
- 🔄 Multi-LLM Orchestration (Gemini, Ollama, Hugging Face)
- 🔐 Secure Authentication (Argon2 + Pepper Secret)
- 📊 Vector Search with pgvector
- 🤖 Telegram Bot Integration
- 📈 Performance Monitoring & Benchmarking
- 🧩 Modular Architecture with Clean Separation
- ⚙️ Configurable Clustering Algorithms (HDBSCAN/K-Means)


## Project Structure

```bash
backend/
├── Api/                 # Main API entry point
│   └── RootBootstrapper/ # Startup configuration and middleware
├── Horus.Modules.Core.Application/  # CQRS patterns, services
│   ├── Services/        # Core service interfaces
│   └── Usecases/        # Business logic implementations
├── Horus.Modules.Core.Domain/  # Domain models and contracts
│   ├── Entities/        # Database entities
│   └── Repositories/    # Repository interfaces
├── Horus.Modules.Core.Infra/   # Infrastructure implementations
│   ├── Services/        # Concrete service implementations
│   └── Context/         # Database context and migrations
├── IntegrationTests/    # End-to-end test suite
├── Benchmarks/          # Performance testing
├── Shared/              # Cross-cutting concerns
│   ├── Contracts/       # Shared interfaces
│   └── Infrastructure/  # Common infrastructure components
├── docker-compose.yaml  # Local development environment
└── Makefile             # Build automation
```

## Key Components

### Core Modules
- **Application Layer**: CQRS with MediatR (`Horus.Modules.Core.Application`)
- **Domain Layer**: Entities and business rules (`Horus.Modules.Core.Domain`)
- **Infrastructure Layer**: Database, LLM integrations (`Horus.Modules.Core.Infra`)

### AI Services
- **RAG Implementation**: `Services/RAG/` (HDBSCAN/KMeans clustering)
- **LLM Providers**: `Services/LLMProvider/` (Gemini, Ollama)
- **Embedding Service**: `Services/Embedding/` (Ollama-based)

### Infrastructure
- **PostgreSQL**: Vector storage with pgvector
- **Redis**: Caching and rate limiting
- **RabbitMQ**: Async task processing

## Getting Started

### Prerequisites
- Docker & Docker Compose
- .NET 8.0 SDK
- PostgreSQL 15+ with pgvector extension
- Redis 7+
- RabbitMQ 3.12+
- Ollama (latest version)
- API Keys:
  - Google Gemini
  - Hugging Face
  - Telegram Bot

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=horusdb;Include Error Detail=true",
    "Redis": "localhost:6380,password=your_redis_password,ssl=False",
    "RabbitMQ": "amqp://user:password@localhost:5672"
  },
  "TextProcessingApi": {
    "Host": "http://localhost:8000"
  }
  // ... other sections
}
```

Launch Application:
```bash
dotnet run --project Api/RootBootstrapper --environment Development
```

## Core Configuration

Key sections in `appsettings.json`:

```json
{
  "SecurityOptions": {
    "PepperSecret": "your_pepper_string",
    "Argon2Options": {
      "DegreeOfParallelism": 4,
      "MemorySize": 65536,
      "Iterations": 3
    }
  },
  "RagOptions": {
    "SearchThresold": 0.6,
    "SearchLimit": 5,
    "ChunkStrategies": {
      "FixedChunk": {
        "ChunkSize": 100,
        "Overlap": 20
      },
      "SemanticChunk": {
        "EmbeddingModel": "all-MiniLM-L6-v2",
        "SimilarityThreshold": 0.75,
        "MinClusterSize": 5,
        "MaxClusterSize": 10,
        "AllowOverlappingClusters": false,
        "ClusteringAlgorithm": "HDBSCAN"
      }
    }
  },
  "Ollama": {
    "Url": "http://localhost:11434",
    "ModelName": "qwen2.5:3b",
    "EmbeddingModel": "all-minilm:l6-v2"
  },
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Local Development

1. **Start Infrastructure**:
```bash
docker compose up -d db redis python_services (root dir)
```

2. **Configure Models**:
```bash
ollama pull qwen2.5:3b
ollama pull all-minilm:l6-v2
```

3. **Set Environment Variables**:
```bash
vim .env
# Update with your API keys
```

4. **Run Application**:
```bash
dotnet run --project backend/Api/RootBootstrapper
```

## Configuration

Key configuration files:

| File | Purpose |
|------|---------|
| `appsettings.json` | Main application configuration |
| `docker-compose.yaml` | Service dependencies |
| `.env` | Sensitive credentials |
| `Horus.Modules.Core.Application/Services/*` | Service implementations |

Example RAG configuration:
```json
"RagOptions": {
  "SearchThresold": 0.6,
  "ChunkStrategies": {
    "FixedChunk": {
      "ChunkSize": 100,
      "Overlap": 20
    },
    "SemanticChunk": {
      "EmbeddingModel": "all-MiniLM-L6-v2",
      "ClusteringAlgorithm": "HDBSCAN"
    }
  }
}
```

## Development Workflow

### Testing
- **Unit Tests**: `LuzInga.UnitTests/`
- **Integration Tests**: `LuzInga.IntegrationTests/`
- **Benchmarks**: `Horus.Modules.Core.Benchmarks/`

Run tests:
```bash
make test-unit
make test-integration
```

### Code Generation
The project uses CQRS patterns with automatic endpoint generation:
```csharp
// Example CRUD configuration
builder.Services.AddCrudGenerator(options =>
{
    options.BaseRoute = "/api/v1";
    options.SetterMethodPrefix = "Update";
    options.CrudRoutes.AddRoutes(
        CrudRoute.For<Project>("projects")
            .DisableAntiforgery(),
        CrudRoute.For<ChatSession>("chatsessions")
            .DisableAntiforgery(),
        CrudRoute.For<ChatMessage>("chatmessages")
            .DisableAntiforgery(),
        CrudRoute.For<User>("users")
            .WithCreate(false)
            .WithUpdate(false)
            .DisableAntiforgery(),
        CrudRoute.For<Document>("documents")
            .WithCreate(false)
            .WithUpdate(false)
            .WithRead(true)
            .WithGetAll(true)
            .DisableAntiforgery()
    );
});;
```

## Deployment

### Docker Production Build
```bash
docker-compose -f docker-compose.yml build
docker-compose -f docker-compose.yml up -d
```

### CI/CD Pipeline
GitHub Actions workflow (`.github/workflows/dotnet.yml`):
- Automated testing
- Docker image building
- Security scanning

## Contributing

1. Create feature branch from `main`
2. Follow architecture patterns:
   - Domain logic in `Core.Domain`
   - Infrastructure in `Core.Infra`
   - Business logic in `Core.Application`
3. Update documentation:
   - Add Swagger annotations
   - Update relevant README sections

## License

MIT License © 2024 Horus AI Team

