---
name: Implement RAG System Tests
about: Implementation of unit and integration tests for the RAG system
title: 'test: Implement Unit and Integration Tests for RAG System'
labels: 'testing, database'
assignees: ''
---

# Implement Unit and Integration Tests for RAG System

## Description
Implement comprehensive test suite for the RAG system's infrastructure layer, including unit tests for entity configurations and integration tests for database operations.

## Test Categories

### 1. Unit Tests for Entity Configurations

#### Base Entity Tests
- [ ] Test BaseEntity timestamp behaviors
- [ ] Test BaseEntityWithMetadata JSON serialization
- [ ] Test metadata default values and modifications

#### Entity Configuration Tests
- [ ] Test Project entity configuration
  - Primary key generation (UUID)
  - Required fields validation
  - Metadata JSON conversion
  - Navigation property configuration

- [ ] Test Document entity configuration
  - Foreign key relationships
  - Unique constraints (source_uri)
  - Metadata and timestamp handling
  - Navigation properties loading

- [ ] Test DocumentChunk entity configuration
  - Offset validation constraints
  - TSVector generation
  - Navigation property configuration

- [ ] Test ChunkEmbedding configuration
  - Vector type handling
  - Foreign key relationships
  - HNSW index configuration

- [ ] Test ChatSession and ChatMessage configurations
  - Role enumeration constraints
  - Vector embedding handling
  - Reference array handling

### 2. Integration Tests

#### Database Context Tests
```csharp
[TestFixture]
public class HorusContextTests : IDisposable
{
    private HorusContext _context;
    private PostgreSqlContainer _dbContainer;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        // Setup test container
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithPassword("postgres")
            .WithName($"test-db-{Guid.NewGuid()}")
            .Build();
        
        await _dbContainer.StartAsync();
        
        // Setup database
        var options = new DbContextOptionsBuilder<HorusContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;
            
        _context = new HorusContext(options);
        await _context.Database.MigrateAsync();
    }

    [Test]
    public async Task CreateProject_WithValidData_ShouldSucceed()
    {
        // Arrange
        var project = new Project 
        { 
            Name = "Test Project",
            SystemPrompt = "Test prompt",
            Metadata = new Dictionary<string, object>
            {
                { "key", "value" }
            }
        };

        // Act
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();

        // Assert
        var savedProject = await _context.Projects
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == project.Id);
            
        Assert.That(savedProject, Is.Not.Null);
        Assert.That(savedProject.Name, Is.EqualTo("Test Project"));
        Assert.That(savedProject.Metadata["key"], Is.EqualTo("value"));
    }
}
```

#### Repository Tests
- [ ] Test document creation with chunks
- [ ] Test hybrid search functionality
- [ ] Test chat session management
- [ ] Test embedding model versioning

### 3. Performance Tests
- [ ] Test HNSW index performance with large datasets
- [ ] Test hybrid search with varying thresholds
- [ ] Test bulk document insertion
- [ ] Measure query execution times

## Technical Requirements

### Test Environment Setup
1. **Docker Containers**:
```csharp
public class PostgreSqlContainer : DockerContainer
{
    public PostgreSqlContainer() : base("postgres:16")
    {
        // Configure container
    }

    protected override async Task InitializeAsync()
    {
        // Initialize pgvector and other extensions
        await ExecuteCommandAsync("CREATE EXTENSION IF NOT EXISTS vector;");
        await ExecuteCommandAsync("CREATE EXTENSION IF NOT EXISTS uuid-ossp;");
    }
}
```

2. **Required NuGet Packages**:
```xml
<ItemGroup>
    <PackageReference Include="NUnit" Version="3.13.3" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.4.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.5.0" />
    <PackageReference Include="Testcontainers" Version="3.5.0" />
    <PackageReference Include="FluentAssertions" Version="6.10.0" />
    <PackageReference Include="Moq" Version="4.18.4" />
</ItemGroup>
```

### Test Data Generation
```csharp
public static class TestDataGenerator
{
    public static Project CreateTestProject()
    {
        return new Project
        {
            Name = $"Test Project {Guid.NewGuid()}",
            SystemPrompt = "Test system prompt",
            Metadata = new Dictionary<string, object>
            {
                { "created_by", "test" },
                { "environment", "test" }
            }
        };
    }

    public static Document CreateTestDocument(Guid projectId)
    {
        return new Document
        {
            ProjectId = projectId,
            SourceUri = $"test://document/{Guid.NewGuid()}",
            RawContent = "Test content",
            ProcessedContent = "Processed test content",
            Checksum = "test-checksum",
            ChunkingStrategy = "fixed"
        };
    }
}
```

## Acceptance Criteria
- [ ] All unit tests pass with >90% code coverage
- [ ] Integration tests verify all CRUD operations
- [ ] Performance tests show acceptable query times
- [ ] Test containers properly initialize with required extensions
- [ ] All edge cases and error conditions are tested
- [ ] Documentation for running tests is provided

## Dependencies
- Docker for test containers
- PostgreSQL 16 with pgvector
- .NET 7+ SDK
- NUnit and related testing packages

## Additional Notes
- Tests should run in CI/CD pipeline
- Use test containers for isolation
- Include performance benchmarks in test results
- Document any test data requirements
