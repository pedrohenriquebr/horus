namespace Horus.Modules.Core.Infra.Services.PythonServices;

using Refit;
using System.Threading.Tasks;

public interface ITextProcessingApi
{
    // O método que vai acessar a rota '/get_sentences' na sua API Python
    [Post("/get_sentences")]
    Task<GetSentencesResponse> GetSentencesAsync([Body] GetSentencesRequest request);
}


public class GetSentencesRequest
{
    public string Text { get; set; }
}
// Classe para armazenar a resposta do endpoint
public class GetSentencesResponse
{
    public List<string> Sentences { get; set; }
}
