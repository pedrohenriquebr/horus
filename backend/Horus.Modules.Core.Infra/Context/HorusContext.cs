using System.Collections.ObjectModel;
using System.Data;
using System.Text.Json;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Newtonsoft.Json.Serialization;
using Pgvector.EntityFrameworkCore;

namespace Horus.Modules.Core.Infra.Context;

public class HorusContext : DbContext, IHorusContext, IUnitOfWork
{
    private IMediator? _mediator;
    private IDbContextTransaction? _transaction;

    public HorusContext(DbContextOptions<HorusContext> options)
        : base(options)
    {
    }

    public DbSet<EmbeddingModel?> EmbeddingModels { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<HybridSearchResult> HybridSearchResults { get; set; }

    public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;
    public DbSet<ChunkEmbedding> ChunkEmbeddings { get; set; } = null!;
    public DbSet<DocumentStatus> DocumentStatuses { get; set; } = null!;
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<HttpLog> HttpLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    public DbSet<RagExperiment> RagExperiments { get; set; } = null!;

    public IDbConnection Connection => Database.GetDbConnection();

    public void BeginTransaction()
    {
        _transaction = Database.BeginTransaction();
    }

    public async Task CommitTransactionAsync()
    {
        await SaveChangesAsync();
        await _transaction!.CommitAsync();
        await _mediator?.DispatchDomainEventsAsync(this)!;
    }

    public async Task RollbackAsync()
    {
        await _transaction!.RollbackAsync();
    }

    public HorusContext WithMediator(IMediator mediator)
    {
        _mediator = mediator;
        return this;
    }

    public HorusContext DisableMediator()
    {
        _mediator = null;
        return this;
    }
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        


        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("vector");
        
       
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        var metadataConverter = new ValueConverter<IReadOnlyDictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => new ReadOnlyDictionary<string, object>(
                JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ??
                new Dictionary<string, object>()
            ));


        // JSONB Metadata
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(BaseEntityWithMetadata).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property("Metadata")
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(metadataConverter)
                .HasDefaultValue(new Dictionary<string, object>().AsReadOnly());
        }
        
        modelBuilder.Entity<RagExperiment>(entity =>
        {
            entity.ToTable("rag_experiments");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()"); // for Postgres

            entity.Property(e => e.RagOptionsJson)
                .HasColumnName("rag_options_json");

            entity.Property(e => e.AveragePrecision)
                .HasColumnName("average_precision");

            entity.Property(e => e.AverageRecall)
                .HasColumnName("average_recall");

            entity.Property(e => e.AverageF1)
                .HasColumnName("average_f1");
        });

        
        modelBuilder.Entity<HybridSearchResult>(entity =>
        {
            entity.HasNoKey(); // Marks this as keyless
            entity.ToView(null); // Not mapped to a database view/table
            entity.Property(d => d.Metadata)
                .HasColumnName("metadata")
                .HasConversion(metadataConverter);

            entity.Property(d => d.ChunkId)
                .HasColumnName("chunk_id");
            
            entity.Property(d => d.DocumentId)
                .HasColumnName("document_id");
            
            entity.Property(d => d.TsRank)
                .HasColumnName("ts_rank");
            
            entity.Property(d => d.Content)
                .HasColumnName("content");

            entity.Property(d => d.Similarity)
                .HasColumnName("similarity");
        });


        // EmbeddingModel
        modelBuilder.Entity<EmbeddingModel>()
            .HasIndex(em => em.Name)
            .IsUnique();

        modelBuilder.Entity<EmbeddingModel>()
            .HasIndex(em => em.Id);
        
        modelBuilder.Entity<EmbeddingModel>()
            .HasKey(em => em.Id);

        modelBuilder.Entity<EmbeddingModel>()
            .ToTable("embedding_models");

        modelBuilder.Entity<EmbeddingModel>()
            .Property(d => d.Name)
            .HasColumnName("name");
        
        modelBuilder.Entity<EmbeddingModel>()
            .Property(d => d.Version)
            .HasColumnName("version");

        modelBuilder.Entity<EmbeddingModel>()
            .Property(d => d.Dimensions)
            .HasColumnName("dimensions");
        
        modelBuilder.Entity<EmbeddingModel>()
            .Property(d => d.Id)
            .HasColumnName("id");
        
        modelBuilder.Entity<EmbeddingModel>().Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        modelBuilder.Entity<EmbeddingModel>().Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
        
        modelBuilder.Entity<EmbeddingModel>().
            HasMany<ChunkEmbedding>()
            .WithOne(d => d.EmbeddingModel)
            .HasForeignKey(d => d.ModelId);


            

        // Project
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.CreatedAt);

        // Document
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.ProjectId);

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.SourceUri)
            .IsUnique();

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.StatusId);

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.Metadata)
            .HasMethod("gin");

        // DocumentChunk
        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(dc => dc.DocumentId);

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(dc => dc.SearchVector)
            .HasMethod("gin");

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(dc => new { dc.StartOffset, dc.EndOffset });

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(dc => dc.ChunkNumber);

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(dc => dc.StatusId);

        modelBuilder.Entity<DocumentChunk>()
            .HasOne(dc => dc.Embedding)
            .WithOne()
            .HasForeignKey<ChunkEmbedding>(ce => ce.ChunkId);

        // ChunkEmbedding (HNSW Index)
        modelBuilder.Entity<ChunkEmbedding>()
            .HasIndex(ce => ce.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        modelBuilder.Entity<ChunkEmbedding>()
            .HasIndex(ce => ce.ModelId);
        
        modelBuilder.Entity<ChunkEmbedding>()
            .HasOne(d => d.EmbeddingModel)
            .WithMany()
            .HasForeignKey(ce => ce.ModelId);
        
        modelBuilder.Entity<ChunkEmbedding>()
            .HasKey(d => d.ChunkId);
        
        modelBuilder.Entity<ChunkEmbedding>()
            .Property(d => d.ChunkId)
            .HasColumnName("chunk_id");

        // ChatSession
        modelBuilder.Entity<ChatSession>()
            .HasIndex(cs => cs.ProjectId);

        modelBuilder.Entity<ChatSession>()
            .HasIndex(cs => cs.CreatedAt);

        // ChatMessage
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(cm => cm.SessionId);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(cm => cm.CreatedAt);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(cm => cm.QueryEmbedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        // HttpLog
        modelBuilder.Entity<HttpLog>()
            .HasIndex(hl => hl.CreatedAt);

        modelBuilder.Entity<HttpLog>()
            .HasIndex(hl => hl.StatusCode);

        modelBuilder.Entity<HttpLog>()
            .HasIndex(hl => hl.Service);

        modelBuilder.Entity<HttpLog>()
            .HasIndex(hl => hl.Metadata)
            .HasMethod("gin");

        modelBuilder.Entity<HttpLog>()
            .Property(hl => hl.RequestHeaders)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions(JsonSerializerDefaults.General)),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v,
                    new JsonSerializerOptions(JsonSerializerDefaults.General))!);

        modelBuilder.Entity<HttpLog>()
            .Property(hl => hl.ResponseHeaders)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions(JsonSerializerDefaults.General)),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v,
                    new JsonSerializerOptions(JsonSerializerDefaults.General))!);

        // AuditLog
        modelBuilder.Entity<AuditLog>()
            .Property(al => al.CreatedAt)
            .HasColumnName("timestamp");

        modelBuilder.Entity<AuditLog>()
            .Property(al => al.EventData)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions(JsonSerializerDefaults.General)),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v,
                    new JsonSerializerOptions(JsonSerializerDefaults.General))!);

        // Ignore UpdatedAt when not defined
        modelBuilder.Entity<DocumentChunk>().Ignore(dc => dc.UpdatedAt);
        modelBuilder.Entity<ChunkEmbedding>().Ignore(ce => ce.UpdatedAt);
        modelBuilder.Entity<ChatSession>().Ignore(cs => cs.UpdatedAt);
        modelBuilder.Entity<ChatMessage>().Ignore(cm => cm.UpdatedAt);
        modelBuilder.Entity<AuditLog>().Ignore(al => al.UpdatedAt);
        modelBuilder.Entity<EmbeddingModel>().Ignore(al => al.UpdatedAt);


        //entities properties mapping
        modelBuilder.Entity<Document>(builder =>
        {
            builder.ToTable("documents");
            builder.HasKey(d => d.Id);

            builder.Metadata.FindNavigation(nameof(Document.Chunks))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.Property(d => d.Id)
                .HasColumnName("id")
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(d => d.ProjectId)
                .HasColumnName("project_id");

            builder.Property(d => d.SourceUri)
                .HasColumnName("source_uri")
                .HasMaxLength(1024)
                .IsRequired();

            builder.Property(d => d.RawContent)
                .HasColumnName("raw_content")
                .IsRequired();

            builder.Property(d => d.ProcessedContent)
                .HasColumnName("processed_content")
                .IsRequired();

            builder.Property(d => d.Checksum)
                .HasColumnName("checksum")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(d => d.StatusId)
                .HasColumnName("status_id")
                .HasDefaultValue(1)
                .IsRequired();

            builder.Property(d => d.ErrorCode)
                .HasColumnName("error_code")
                .HasMaxLength(50);

            builder.Property(d => d.ErrorMessage)
                .HasColumnName("error_message");

            builder.Property(d => d.RetryCount)
                .HasColumnName("retry_count")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(d => d.LastProcessedAt)
                .HasColumnName("last_processed_at");

            builder.Property(d => d.ChunkingStrategy)
                .HasColumnName("chunking_strategy")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(d => d.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(d => d.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasOne<Project>()
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<Project>(builder =>
        {
            builder.ToTable("projects");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();


            builder.Property(p => p.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(p => p.SystemPrompt)
                .HasColumnName("system_prompt")
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
        });
        
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();
            
            builder.Property(p => p.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(p => p.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
            
            builder.Property(p => p.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();
            
            builder.Property(p => p.Salt)
                .HasColumnName("salt")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();
        });

        modelBuilder.Entity<DocumentStatus>(builder =>
        {
            builder.ToTable("document_status");

            builder.Property(ds => ds.Id)
                .HasColumnName("id")
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(ds => ds.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(ds => ds.Description)
                .HasColumnName("description");

            builder.HasIndex(ds => ds.Name)
                .IsUnique();
        });

        modelBuilder.Entity<DocumentChunk>(builder =>
        {
            builder.ToTable("document_chunks");

            builder.HasKey(dc => dc.Id);

            builder.Property(dc => dc.Id)
                .HasColumnName("id")
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(dc => dc.DocumentId)
                .HasColumnName("document_id")
                .IsRequired();

            builder.Property(dc => dc.ChunkNumber)
                .HasColumnName("chunk_number")
                .IsRequired();

            builder.Property(dc => dc.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(dc => dc.StatusId)
                .HasColumnName("status_id");

            builder.Property(dc => dc.ProcessingError)
                .HasColumnName("processing_error");

            builder.Property(dc => dc.EmbeddingError)
                .HasColumnName("embedding_error");

            builder.Property(dc => dc.TokenCount)
                .HasColumnName("token_count")
                .IsRequired();

            builder.Property(dc => dc.StartOffset)
                .HasColumnName("start_offset")
                .IsRequired();

            builder.Property(dc => dc.EndOffset)
                .HasColumnName("end_offset")
                .IsRequired();

            builder.Property(dc => dc.LanguageCode)
                .HasColumnName("language_code")
                .IsRequired();

            builder.Property(dc => dc.SearchVector)
                .HasColumnName("search_vector")
                .HasColumnType("tsvector");

            builder
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN"); // Index method on the search vector (GIN or GIST)

            builder.Property(dc => dc.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
            
            builder.HasOne<Document>()
                .WithMany(d => d.Chunks)
                .HasForeignKey(dc => dc.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasCheckConstraint("valid_offsets", "start_offset < end_offset");
            builder.HasCheckConstraint("CK_token_count_positive", "token_count > 0");
        });

        modelBuilder.Entity<ChunkEmbedding>(builder =>
        {
            builder.ToTable("chunk_embeddings");

            builder.Property(ce => ce.ChunkId)
                .HasColumnName("chunk_id");

            builder.Property(ce => ce.ModelId)
                .HasColumnName("model_id")
                .IsRequired();

            builder.Property(ce => ce.Embedding)
                .HasColumnName("embedding")
                .IsRequired();

            builder.Property(ce => ce.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.HasKey(ce => ce.ChunkId);

            builder.HasOne(ce => ce.EmbeddingModel)
                .WithMany()
                .HasForeignKey(ce => ce.ModelId);
        });

        modelBuilder.Entity<ChatSession>(builder =>
        {
            builder.ToTable("chat_sessions");
            
            builder.HasKey(d => d.Id);

            builder.Property(cs => cs.Id)
                .HasColumnName("id")
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(cs => cs.ProjectId)
                .HasColumnName("project_id");

            builder.Property(cs => cs.Title)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(cs => cs.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(cs => cs.EndedAt)
                .HasColumnName("ended_at");

            builder.HasOne(cs => cs.Project)
                .WithMany(p => p.ChatSessions)
                .HasForeignKey(cs => cs.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(cs => cs.Messages)
                .WithOne()
                .HasForeignKey(cm => cm.SessionId);
        });

        modelBuilder.Entity<ChatMessage>(builder =>
        {
            builder.ToTable("chat_messages");
            
            builder.HasKey(cm => cm.Id);

            builder.Property(cm => cm.Id)
                .HasColumnName("id")
                .ValueGeneratedNever()
                .IsRequired();

            builder.Property(cm => cm.SessionId)
                .HasColumnName("session_id")
                .IsRequired();

            builder.Property(cm => cm.Role)
                .HasColumnName("role")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(cm => cm.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(cm => cm.QueryEmbedding)
                .HasColumnName("query_embedding");

            builder.Property(cm => cm.ReferenceChunkIds)
                .HasColumnName("reference_chunk_ids")
                .HasColumnType("UUID[]");

            builder.Property(cm => cm.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.HasCheckConstraint("CK_role_values", "role IN ('user', 'assistant', 'system')");
        });

        modelBuilder.Entity<HttpLog>(builder =>
        {
            builder.ToTable("http_logs");
            builder.HasKey(hl => hl.Id);

            builder.Property(hl => hl.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();


            builder.Property(hl => hl.Url)
                .HasColumnName("url")
                .IsRequired();

            builder.Property(hl => hl.Method)
                .HasColumnName("method")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(hl => hl.RequestHeaders)
                .HasColumnName("request_headers")
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(hl => hl.RequestBody)
                .HasColumnName("request_body");

            builder.Property(hl => hl.ResponseHeaders)
                .HasColumnName("response_headers")
                .HasColumnType("jsonb");

            builder.Property(hl => hl.ResponseBody)
                .HasColumnName("response_body");

            builder.Property(hl => hl.StatusCode)
                .HasColumnName("status_code");

            builder.Property(hl => hl.ErrorMessage)
                .HasColumnName("error_message");

            builder.Property(hl => hl.DurationMs)
                .HasColumnName("duration_ms");

            builder.Property(hl => hl.Service)
                .HasColumnName("service")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(hl => hl.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(hl => hl.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasCheckConstraint("idx_http_created_check", "created_at <= updated_at");
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(al => al.Id);

            builder.Property(al => al.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();


            builder.Property(al => al.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(al => al.EventData)
                .HasColumnName("event_data")
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(al => al.CreatedAt)
                .HasColumnName("timestamp")
                .IsRequired();

            builder.Property(al => al.UserId)
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.LastProcessedAt);

        modelBuilder.Entity<Document>()
            .HasIndex(d => d.RetryCount);
    }
}