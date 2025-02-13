using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Infra.Services.Repositories;

public interface IDocumentsRepository
{
    // Método para obter um documento por id
    Task<Document?> GetAsync(string id);

    // Método para obter todos os documentos com filtragem e limite
    Task<IEnumerable<Document>> GetAllMemoriesByUserId(string userId, int limit = 5);

    // Método para inserir um novo documento
    Task<Document> InsertAsync(Document data);

    // Método para atualizar um documento existente
    Task<Document> UpdateAsync(string id, Document data);

    // Método para deletar um documento pelo id
    Task DeleteAsync(string id);

    // Método para buscar documentos semelhantes com base em um embedding
    Task<IEnumerable<Document>> MatchDocumentsAsync(float[] embedding, int limit = 5, float threshold = 0.5f);
    Task DeleteAllDocumentsByUserId(string userId, DocumentType documentType);
    Task<Document?> FindByContentAsync(string content);
}