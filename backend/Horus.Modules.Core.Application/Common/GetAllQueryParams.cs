using Microsoft.AspNetCore.Mvc;

namespace Horus.Modules.Core.Application.Common;

public class GetAllQueryParams
{
    [FromQuery(Name = "filter")]
    public string? Filter { get; set; }
    
    // Adicione outras propriedades conforme necessário
}