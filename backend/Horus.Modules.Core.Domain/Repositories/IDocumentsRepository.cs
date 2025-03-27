using Horus.Modules.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Horus.Modules.Core.Domain.Repositories;

public interface IDocumentsRepository : IRepository<Document, Guid>
{

    // Método para obter todos os documentos com filtragem e limite
    Task<IEnumerable<Document>> GetAllMemoriesByUserId(string userId, int limit = 5);

    // Método para buscar documentos semelhantes com base em um embedding
    Task<IEnumerable<Document>> MatchDocumentsAsync(float[] embedding, int limit = 5, float threshold = 0.5f);

    Task<IEnumerable<HybridSearchResult>> MatchDocumentsHybridAsync(float[] embedding, string query, Guid? userId = null, int limit = 5,
        float threshold = 0.5f);
    Task DeleteAllDocumentsByUserId(string userId, DocumentType? documentType=null);
    Task<Document?> FindByContentAsync(string content);
    Task<IEnumerable<Document>> GetByStatusAsync(int statusId);
    Task<IEnumerable<Document>> GetByProjectIdAsync(Guid projectId);
}