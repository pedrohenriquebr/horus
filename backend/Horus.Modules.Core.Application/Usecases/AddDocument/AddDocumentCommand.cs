using Horus.Modules.Core.Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Http;

namespace Horus.Modules.Core.Application.Usecases.AddDocument;

public class AddDocumentCommand : ICommand
{
    public Guid? ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; }
    public string SourceUri { get; set; }
    public Stream FileContent { get; set; }
    public string ChunkingStrategy { get; set; } = "fixed_size";
}

public class AddDocumentCommandRequest
{
    public Guid? ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; }
    public string SourceUri { get; set; }
    public IFormFile FileContent { get; set; }
    public string ChunkingStrategy { get; set; } = "fixed_size";
}
