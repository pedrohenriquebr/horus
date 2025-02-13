using System.Data;
using System.Text.Json;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Horus.Modules.Core.Infra.Context;

public class HorusContext : DbContext, IHorusContext, IUnitOfWork
{
    private IMediator? _mediator;
    private IDbContextTransaction _transaction;

    public HorusContext(DbContextOptions<HorusContext> options)
        : base(options)
    {
    }

    // Propriedade DbSet para a tabela de documentos
    public DbSet<Document> Documents { get; set; }

    public IDbConnection Connection => Database.GetDbConnection();


    public void BeginTransaction()
    {
        _transaction = Database.BeginTransaction();
    }

    public async Task CommitTransactionAsync()
    {
        await SaveChangesAsync();
        await _transaction.CommitAsync();
        await _mediator?.DispatchDomainEventsAsync(this)!;
    }

    public async Task RollbackAsync()
    {
        await _transaction.RollbackAsync();
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

    // Configuração do modelo no OnModelCreating
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");

        // Configura o nome correto da tabela, se necessário
        modelBuilder.Entity<Document>()
            .ToTable("documents"); // Especifica o nome correto da tabela

        modelBuilder.Entity<Document>()
            .HasKey(d => d.Id); // Define a chave primária

        modelBuilder.Entity<Document>()
            .Property(d => d.Id)
            .HasColumnName("id") // Garante que a coluna no banco de dados se chame 'id'
            .HasColumnType("SERIAL") // Especifica o tipo SERIAL para o PostgreSQL
            .ValueGeneratedOnAdd(); // Garantir que o valor é gerado pelo banco de dados (PostgreSQL)


        // Configura a propriedade 'Content'
        modelBuilder.Entity<Document>()
            .Property(d => d.Content)
            .HasColumnName("content") // Mapeia a coluna 'content' no banco de dados
            .HasColumnType("text"); // Define o tipo de coluna como texto

        // Configura a propriedade 'Metadata'
        modelBuilder.Entity<Document>()
            .Property(d => d.Metadata)
            .HasColumnName("metadata") // Mapeia a coluna 'metadata'
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions(JsonSerializerDefaults.General)),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v,
                    new JsonSerializerOptions(JsonSerializerDefaults.General))!);
        ; // Define o tipo da coluna como JSONB

        // Configura a propriedade 'Embedding'
        modelBuilder.Entity<Document>()
            .Property(d => d.Embedding)
            .HasColumnName("embedding") // Mapeia a coluna 'embedding'
            .HasColumnType("vector(384)"); // Define o tipo da coluna como vetor de dimensão 384


        // Configura a propriedade 'CreatedAt'
        modelBuilder.Entity<Document>()
            .Property(d => d.CreatedAt)
            .HasColumnName("created_at") // Mapeia a coluna 'created_at'
            .HasColumnType("timestamptz") // Define o tipo da coluna como TIMESTAMP com fuso horário
            .HasDefaultValueSql("CURRENT_TIMESTAMP"); // Define o valor padrão para a data de criação

        // Se você quiser garantir que outras propriedades sejam mapeadas corretamente
        // você pode seguir o mesmo padrão de mapeamento.
    }
}