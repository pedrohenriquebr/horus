using FluentValidation;

namespace Horus.Modules.Core.Application.Usecases.AddDocument;

public class AddDocumentCommandValidator : AbstractValidator<AddDocumentCommand>
{

    public static readonly string[] VALID_CHUNK_STRATEGIES = new string[]
    {
        "fixed_chunk", "semantic_chunk"
    };
    
    public AddDocumentCommandValidator()
    {
        RuleFor(x=> x.ChunkingStrategy)
            .NotNull()
            .NotEmpty()
            .WithMessage("Chunking strategy is required");
        
        RuleFor(x => x.ChunkingStrategy)
            .Must(x => VALID_CHUNK_STRATEGIES.Contains(x))
            .WithMessage("Chunking strategy is invalid, valid strategies are: " + string.Join(", ", VALID_CHUNK_STRATEGIES));
    }
}