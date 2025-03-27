namespace Horus.Modules.Core.Domain.Entities;

public class EmbeddingModel : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int Dimensions { get; set; }
}