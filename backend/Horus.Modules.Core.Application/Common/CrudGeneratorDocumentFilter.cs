using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Horus.Modules.Core.Application.Common;

public class CrudGeneratorDocumentFilter : IDocumentFilter
{
    private readonly CrudGeneratorService _crudGeneratorService;

    public CrudGeneratorDocumentFilter(CrudGeneratorService crudGeneratorService)
    {
        _crudGeneratorService = crudGeneratorService;
    }
    
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        _crudGeneratorService.RegisterSchemas(swaggerDoc, context);
    }
}