using System.Data;
using Horus.Modules.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Horus.Modules.Core.Domain;

public interface IHorusContext
{
    public IDbConnection Connection { get; }
    
    public DbSet<EmbeddingModel?> EmbeddingModels { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<ChunkEmbedding> ChunkEmbeddings { get; set; }
    public DbSet<DocumentStatus> DocumentStatuses { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<HttpLog> HttpLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    public ChangeTracker  ChangeTracker { get; }
}

public interface IUnitOfWork
{
    public void BeginTransaction();
    public Task CommitTransactionAsync();
    public Task RollbackAsync();
}