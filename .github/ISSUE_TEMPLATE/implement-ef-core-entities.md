---
name: Implement Entity Framework Core Domain Models
about: Implementation of EF Core entities for the RAG system
title: 'feat: Implement Entity Framework Core Domain Models for RAG System'
labels: 'enhancement, database'
assignees: ''
---

# Implement Entity Framework Core Domain Models for RAG System

## Description
Implement the complete set of entity models and their configurations for the RAG system using Entity Framework Core, following the existing database schema and maintaining proper relationships and configurations.

## Tasks

### 1. Create Base Entity Classes
- [ ] Implement `BaseEntity` with common properties (CreatedAt, UpdatedAt)
- [ ] Implement `BaseEntityWithMetadata` extending `BaseEntity` to include JSONB metadata

### 2. Update Existing Document Entity
- [ ] Update the `Document` entity to match new schema with:
  - Project relationship
  - Source URI
  - Raw and processed content
  - Checksum
  - Chunking strategy

### 3. Implement New Entities
- [ ] Create `Project` entity
- [ ] Create `DocumentChunk` entity
- [ ] Create `EmbeddingModel` entity
- [ ] Create `ChunkEmbedding` entity
- [ ] Create `ChatSession` entity
- [ ] Create `ChatMessage` entity
- [ ] Create `HttpLog` entity
- [ ] Create `AuditLog` entity

### 4. Update DbContext Configuration
- [ ] Add DbSets for all new entities
- [ ] Configure relationships and constraints
- [ ] Set up indexes including HNSW and GIN
- [ ] Configure value converters for special types (Vector, JSONB)

## Implementation Details

Here's a sample of how the entities should be implemented:

```csharp
// Base entities
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public abstract class BaseEntityWithMetadata : BaseEntity
{
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Project entity
public class Project : BaseEntityWithMetadata
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}

// Updated Document entity
public class Document : BaseEntityWithMetadata
{
    public long Id { get; set; }
    public Guid ProjectId { get; set; }
    public string SourceUri { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public string ProcessedContent { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public string ChunkingStrategy { get; set; } = string.Empty;

    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}

// Document Chunk entity
public class DocumentChunk : BaseEntityWithMetadata
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public int ChunkNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
    public string SearchVector { get; set; } = string.Empty;

    // Navigation properties
    public virtual Document Document { get; set; } = null!;
    public virtual ChunkEmbedding? Embedding { get; set; }
}
```

## Technical Notes

1. **Special Type Handling**:
   - Vector type needs custom value converter
   - JSONB needs custom value converter using System.Text.Json
   - TSVector needs custom handling

2. **Index Configuration Example**:
```csharp
modelBuilder.Entity<ChunkEmbedding>()
    .HasIndex(e => e.Embedding)
    .HasMethod("hnsw")
    .HasOperators("vector_cosine_ops");
```

3. **Required NuGet Packages**:
   - Npgsql.EntityFrameworkCore.PostgreSQL
   - Pgvector.EntityFrameworkCore

## Acceptance Criteria
- [ ] All entities properly mapped to database schema
- [ ] All relationships correctly configured
- [ ] Special PostgreSQL types (vector, tsvector, jsonb) properly handled
- [ ] Indexes correctly configured including HNSW and GIN
- [ ] Unit tests for entity configurations
- [ ] Integration tests with test database

## Dependencies
- PostgreSQL 16+
- pgvector extension
- Entity Framework Core 7.0+

## Additional Information
This implementation is part of the larger RAG system implementation. The entities should follow the database schema defined in the main issue and maintain compatibility with pgvector 0.6+.
