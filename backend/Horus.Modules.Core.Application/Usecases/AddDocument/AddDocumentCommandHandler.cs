using System.Text;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Domain.Factories;
using MimeDetective;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Horus.Modules.Core.Application.Usecases.AddDocument;

public class AddDocumentCommandHandler : ICommandHandler<AddDocumentCommand>
{
    private readonly IRagService _ragService;
    private readonly IDocumentFactory _documentFactory;

    public AddDocumentCommandHandler(IRagService ragService, IDocumentFactory documentFactory)
    {
        _ragService = ragService;
        _documentFactory = documentFactory;
    }

    public async Task Handle(AddDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1) Copiar o FileContent para MemoryStream (caso ainda não seja)
        using var memoryStream = new MemoryStream();
        request.FileContent.CopyTo(memoryStream);
        memoryStream.Position = 0;

        // 2) Detectar o MIME type com Mime-Detective
        var inspector = new ContentInspectorBuilder() {
            Definitions = MimeDetective.Definitions.DefaultDefinitions.All()
        }.Build();

        var inspectionResult = inspector
            .Inspect(memoryStream)
            .FirstOrDefault();
        var mimeType = "application/octet-stream";
        if (inspectionResult != null)
        {
            mimeType = inspectionResult.Definition?.File.MimeType ?? "application/octet-stream";
        }

        // 2) Ler o conteúdo de acordo com o tipo
        var content = ReadFileByMimeType(memoryStream, mimeType);

        // 3) Calcular um checksum (opcional)
        var checksum = CheckSum(content);

        // 4) Criar entidade e enviar para o RAG
        var document = _documentFactory.CreatePending(
            request.SourceUri,
            content,
            checksum,
            request.ChunkingStrategy
        );

        if (request.UserId != null)
        {
            var metadata = new Dictionary<string, object>()
            {
                { "userId", request.UserId }
            };
            document.SetMetadata(metadata);
        }

        await _ragService.IngestAsync(document);
        await Task.CompletedTask;
    }

    private static string ReadFileByMimeType(Stream stream, string mimeType)
    {
        stream.Position = 0; // garante que vamos ler desde o início

        switch (mimeType.ToLowerInvariant())
        {
            case "application/pdf":
                return ExtractPdfText(stream);

            case "text/plain":
                return ExtractPlainText(stream);

            default:
                // Caso não reconheça, tratar como binário ou retornar erro
                // Exemplo: jogar uma exceção ou só tratar como texto
                // Dependendo do seu caso, você pode ler como string ou 
                // simplesmente retornar string vazia
                return ExtractPlainText(stream);
        }
    }

    private static string ExtractPdfText(Stream pdfStream)
    {
        // Usa PdfPig para extrair texto
        using var pdfDoc = PdfDocument.Open(pdfStream);
        var sb = new StringBuilder();

        foreach (Page page in pdfDoc.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractPlainText(Stream textStream)
    {
        textStream.Position = 0;
        using var reader = new StreamReader(textStream, Encoding.UTF8, leaveOpen: true);
        string allText = reader.ReadToEnd();
        return allText;
    }

    public static string CheckSum(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes); // .NET 5+
    }
}
