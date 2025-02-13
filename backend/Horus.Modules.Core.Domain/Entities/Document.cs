using Pgvector;

namespace Horus.Modules.Core.Domain.Entities;

public class Document
{
    public long Id { get; set; } // Identificador único (gerado automaticamente)
    public string Content { get; set; } = string.Empty; // Conteúdo do documento
    public Dictionary<string, object> Metadata { get; set; } // Metadados associados ao documento
    public Vector? Embedding { get; set; } = new("[0]"); // Vetor de embeddings (utilizado para buscas similares)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Data de criação do documento

    // Se você precisar configurar o mapeamento do campo Embedding com mais detalhes, pode fazê-lo no OnModelCreating no contexto.
}