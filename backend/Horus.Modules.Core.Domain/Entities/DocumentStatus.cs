namespace Horus.Modules.Core.Domain.Entities;

public enum DocumentStatusEnum
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
}
public class DocumentStatus : BaseEntity
{
    public int Id { get; private init; }
    public string Name { get; private init; }
    public string Description { get; private init; }

    private DocumentStatus() { }

    public static readonly DocumentStatus Pending = new() 
    { 
        Id = 1, 
        Name = "Pending", 
        Description = "Document received, waiting for processing" 
    };
    
    public static readonly DocumentStatus Processing = new() 
    { 
        Id = 2, 
        Name = "Processing", 
        Description = "Document processing, waiting for completion" 
    };
    
    public static readonly DocumentStatus Completed = new() 
    { 
        Id = 3, 
        Name = "Completed", 
        Description = "Document processed successfully"
    };
    
    public static readonly DocumentStatus Failed = new() 
    { 
        Id = 4, 
        Name = "Failed", 
        Description = "Failed to process document"
    };
}