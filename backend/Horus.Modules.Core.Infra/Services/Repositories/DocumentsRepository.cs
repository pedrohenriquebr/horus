using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Core.Infra.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Infra.Services.Repositories;

public class DocumentsRepository : IDocumentsRepository
{
    private readonly HorusContext _context;
    private readonly ILogger<DocumentsRepository> _logger;

    public DocumentsRepository(HorusContext context, ILogger<DocumentsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Document?> GetAsync(string id)
    {
        try
        {
            return await _context.Documents.FindAsync(long.Parse(id));
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
            WHERE metadata->>'user_id' = {0}
              AND metadata->>'type' = 'memory'
            LIMIT {1};";

            // Executa o comando SQL no banco de dados usando parâmetros
            var result = await _context.Documents
                .FromSqlRaw(query, userId, limit)
                .ToListAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory documents with userId {userId}", userId);
            return Enumerable.Empty<Document>();
        }
    }


    public async Task<Document> InsertAsync(Document data)
    {
        try
        {
            _context.Documents.Add(data);
            await _context.SaveChangesAsync();
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting document");
            throw;
        }
    }

    public async Task<Document> UpdateAsync(string id, Document data)
    {
        try
        {
            var existing = await GetDocumentByIdAsync(id);
            _context.Entry(existing).CurrentValues.SetValues(data);
            await _context.SaveChangesAsync();
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document with id {id}", id);
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        try
        {
            var document = await GetDocumentByIdAsync(id);
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
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
            var results = await _context.Documents.FromSqlRaw(
                "SELECT * FROM match_documents(@embedding, @limit, @threshold)",
                new SqlParameter("@embedding", embedding),
                new SqlParameter("@limit", limit),
                new SqlParameter("@threshold", threshold)
            ).ToListAsync();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching documents");
            return Enumerable.Empty<Document>();
        }
    }

    public async Task DeleteAllDocumentsByUserId(string userId, DocumentType documentType)
    {
        try
        {
            var query = @"
            DELETE FROM documents 
            WHERE metadata->>'user_id' = {0}
            AND metadata->>'type' = {1}";

            await _context.Database.ExecuteSqlRawAsync(query, userId, MapDocumentType(documentType));
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
            var query = "SELECT * FROM documents WHERE content = {0}";
            var results = await _context.Documents.FromSqlRaw(query, content).ToListAsync();
            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching documents");
            return null;
        }
    }

    // Helper method to get document by id
    private async Task<Document> GetDocumentByIdAsync(string id)
    {
        var document = await _context.Documents.FindAsync(long.Parse(id));
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