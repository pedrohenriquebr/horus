using System.Text.Json;
using Supabase.Postgrest.Attributes;

namespace Horus.Modules.Core.Domain;

public class HybridSearchResult
{
    public Guid DocumentId { get; set; }
    public Guid ChunkId { get; set; }
    public string Content { get; set; }
    public double Similarity { get; set; }  // double para DOUBLE PRECISION
    public float TsRank { get; set; }       // float para REAL
    public IReadOnlyDictionary<string,object> Metadata { get; set; } = new Dictionary<string,object>().AsReadOnly();
}