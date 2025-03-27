using System.Linq.Expressions;
using System.Text.Json;
using Horus.Modules.Core.Domain;
using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Domain.Repositories;
using Horus.Modules.Core.Infra.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Horus.Modules.Core.Infra.Services.Repositories;

public class DocumentsRepository : BaseRepository, IDocumentsRepository
{
    private readonly HorusContext _context;
    private readonly ILogger<DocumentsRepository> _logger;

    public DocumentsRepository(HorusContext context, ILogger<DocumentsRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Document?> GetAsync(Guid id)
    {
        try
        {
            return await _context.Documents.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document with id {id}", id);
            return null;
        }
    }

    public async Task<IEnumerable<Document>> GetAllMemoriesByUserId(string userId, int limit = 5)
    {
        try
        {
            // Valida se o userId foi fornecido
            if (string.IsNullOrEmpty(userId)) return Enumerable.Empty<Document>();

            // Consulta SQL para buscar documentos com 'user_id' e 'type' em 'metadata'
            var query = @"
            SELECT * 
            FROM documents
            WHERE metadata->>'userId' = {0}
              AND metadata->>'type' = 'memory'
            LIMIT {1};";
            
            // Executa o comando SQL no banco de dados usando parâmetros
            var result = await EntityFrameworkQueryableExtensions
                .ToListAsync<Document>(_context.Documents
                    .FromSqlRaw(query, userId, limit));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory documents with userId {userId}", userId);
            return Enumerable.Empty<Document>();
        }
    }


    public Task<Document> InsertAsync(Document data)
    {
        try
        {
            _context.Documents.Add(data);
            return Task.FromResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting document");
            throw;
        }
    }

    public Task<Document> UpdateAsync(Document data)
    {
        try
        {
            _context.Documents.Update(data);
            return Task.FromResult(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document with id {id}", data.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var document = await GetDocumentByIdAsync(id);
            _context.Documents.Remove(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document with id {id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Document>> MatchDocumentsAsync(float[] embedding, int limit = 5,
        float threshold = 0.5f)
    {
        try
        {
            var results = await EntityFrameworkQueryableExtensions.ToListAsync<Document>(_context.Documents.FromSqlRaw(
                "SELECT * FROM hybrid_rag_search(@embedding, @limit, @threshold)",
                new SqlParameter("@embedding", embedding),
                new SqlParameter("@limit", limit),
                new SqlParameter("@threshold", threshold)
            ));
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching documents");
            return Enumerable.Empty<Document>();
        }
    }
    
    public async Task<IEnumerable<HybridSearchResult>> MatchDocumentsHybridAsync(float[] embedding, string query, Guid? userId = null, int limit = 5,
        float threshold = 0.5f)
    {
        try
        {
            var results = await _context.Set<HybridSearchResult>()
                .FromSqlRaw(
                    "SELECT * FROM hybrid_rag_search(@embedding::vector(384), @query::text, @limit::int, @threshold::float, @userId::UUID, NULL::UUID)",
                    new NpgsqlParameter("@embedding", embedding),
                    new NpgsqlParameter("@query", query),
                    new NpgsqlParameter("@limit", limit),
                    new NpgsqlParameter("@threshold", threshold),
                    new NpgsqlParameter("@userId", userId)

                )
                .ToListAsync();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching documents");
            return Enumerable.Empty<HybridSearchResult>();
        }
    }

    public async Task DeleteAllDocumentsByUserId(string userId, DocumentType? documentType=null)
    {
        try
        {
            if (documentType != null)
            {
                var query = @"
            DELETE FROM documents 
            WHERE metadata->>'userId' = {0}
            AND metadata->>'type' = {1}";

                await _context.Database.ExecuteSqlRawAsync(query, userId, MapDocumentType(documentType!.Value));
            }
            else
            {
                var query = @"
            DELETE FROM documents 
            WHERE metadata->>'userId' = {0}";

                await _context.Database.ExecuteSqlRawAsync(query, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting documents for user {userId}", userId);
        }
    }

    public async Task<Document?> FindByContentAsync(string content)
    {
        try
        {
            var query = "SELECT * FROM documents WHERE processed_content = {0}";
            var results = await EntityFrameworkQueryableExtensions.ToListAsync<Document>(_context.Documents.FromSqlRaw(query, content));
            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching documents");
            return null;
        }
    }

    public async Task<IEnumerable<Document>> GetByStatusAsync(int statusId)
    {
        var list = await this._context.Documents.Where(d => d.StatusId == statusId).ToListAsync();
        return list;
    }

    public async Task<IEnumerable<Document>> GetByProjectIdAsync(Guid projectId)
    {
        var list = await this._context.Documents.Where(d => d.ProjectId == projectId).ToListAsync();
        return list.AsEnumerable();
    }

    // Helper method to get document by id
    private async Task<Document> GetDocumentByIdAsync(Guid id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null) throw new Exception($"Document with ID {id} not found.");
        return document;
    }


    private string MapDocumentType(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.Memory => "memory",
            _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null)
        };
    }
}