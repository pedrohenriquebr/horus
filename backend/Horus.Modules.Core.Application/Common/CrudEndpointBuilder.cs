using System.Reflection;
using System.Text.Json;
using FluentValidation;
using Horus.Modules.Core.Application.Common.Helpers;
using Horus.Modules.Core.Domain.Entities;
using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Horus.Modules.Core.Application.Common;

public class CrudEndpointBuilder
{
    private readonly WebApplication _app;
    private readonly string _baseRoute;
    private bool _enableCreate = true;
    private bool _enableRead = true;
    private bool _enableUpdate = true;
    private bool _enableDelete = true;
    private bool _enableGetAll = true;

    /// <summary>
    /// Indicates whether antiforgery checks should be disabled for the CRUD endpoints.
    /// </summary>
    private bool _disableAntiforgery = false;

    private readonly string apiBaseRoute;
    private readonly CrudRoute _crudRoute;

    public CrudEndpointBuilder(WebApplication app, string apiBaseRoute, string baseRoute, CrudRoute crudRoute)
    {
        _app = app;
        this._crudRoute = crudRoute;
        _baseRoute = baseRoute;
        this.apiBaseRoute = apiBaseRoute;
    }

    public CrudEndpointBuilder DisableCreate()
    {
        _enableCreate = false;
        return this;
    }

    public CrudEndpointBuilder DisableRead()
    {
        _enableRead = false;
        return this;
    }

    public CrudEndpointBuilder DisableUpdate()
    {
        _enableUpdate = false;
        return this;
    }

    public CrudEndpointBuilder DisableDelete()
    {
        _enableDelete = false;
        return this;
    }

    public CrudEndpointBuilder DisableGetAll()
    {
        _enableGetAll = false;
        return this;
    }

    public CrudEndpointBuilder DisableAntiforgery()
    {
        _disableAntiforgery = true;
        return this;
    }

    public void Register()
    {
        var entityName = _crudRoute.EntityType.Name;

        if (_enableCreate)
        {
            GenerateCreateEndpoint(entityName);
        }

        if (_enableRead)
        {
            GenerateGetByIdEndpoint(entityName);
        }

        if (_enableGetAll)
        {
            GenerateGetAllEndpoint(entityName);
        }

        if (_enableUpdate)
        {
            GenerateUpdateEndpoint(entityName);
        }

        if (_enableDelete)
        {
            GenerateDeleteEndpoint(entityName);
        }
    }

    private void GenerateDeleteEndpoint(string entityName)
    {
        var entityType = _crudRoute.EntityType;
        var routeHandler = _app.MapDelete($"{this.apiBaseRoute}/{_baseRoute}/{{id}}", async (
                string id,
                IMediator mediator) =>
            {
                try
                {
                    // Encontrar propriedade chave
                    var keyProperty = GetKeyProperty(entityType);
                    if (keyProperty == null) throw new InvalidOperationException("Key property not found");

                    // Criar comando genérico
                    var commandType = typeof(DeleteCommand<>).MakeGenericType(entityType);
                    var command = Activator.CreateInstance(commandType);
                    
                    // Converter e atribuir ID
                    object convertedId = ConvertKeyPropertyId(keyProperty, id);
                    ReflectionExtensions.SetProperty(command, "Id", convertedId);

                    await mediator.Send(command);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return HandleException(ex);
                }
            })
            .WithName($"Delete{entityName}")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(500);
    }

    private static object ConvertKeyPropertyId(PropertyInfo keyProperty, string id)
    {
        return keyProperty.PropertyType.Name switch
        {
            "Guid" => Guid.Parse(id),
            "Int" => int.Parse(id),
            "Long" => long.Parse(id),
            "DateTime" => DateTime.Parse(id),
            "DateTimeOffset" => DateTimeOffset.Parse(id),
            "Decimal" => decimal.Parse(id),
            "Double" => double.Parse(id),
            "Float" => float.Parse(id),
            "Boolean" => bool.Parse(id),
            "Byte" => byte.Parse(id),
            "Short" => short.Parse(id),
            "Char" => char.Parse(id),
            _ => throw new InvalidOperationException("Invalid key property type")
        };
    }

    private void GenerateUpdateEndpoint(string entityName)
    {
        var entityType = _crudRoute.EntityType;
        var routeHandler = _app.MapPut($"{this.apiBaseRoute}/{_baseRoute}/{{id}}", async (
                string id,
                IMediator mediator,
                [FromBody] dynamic request) =>
            {
                try
                {
                    var keyProperty = GetKeyProperty(entityType);
                    if (keyProperty == null) throw new InvalidOperationException("Key property not found");

                    // Criar comando genérico
                    var commandType = typeof(UpdateCommand<>).MakeGenericType(entityType);
                    var command = Activator.CreateInstance(commandType);
                    
                    // Converter ID e copiar propriedades
                    var convertedId = ConvertKeyPropertyId(keyProperty, id);
                    ReflectionExtensions.CopyProperties(request, command);
                    ReflectionExtensions.SetProperty(command, "Id", convertedId);
                    Dictionary<string, object> props  = JsonSerializer.Deserialize<Dictionary<string, object>>(request);
                    props.Remove("id");
                    props.Remove("ID");
                    props.Remove("Id");
                    ReflectionExtensions.SetProperty(command, "Properties", props);

                    await mediator.Send(command);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return HandleException(ex);
                }
            })
            .WithName($"Update{entityName}")
            .Produces(204)
            .ProducesProblem(400)
            .ProducesProblem(500);

        if (_disableAntiforgery) routeHandler.DisableAntiforgery();
    }
    
    private void GenerateGetAllEndpoint(string entityName)
    {
        var entityType = _crudRoute.EntityType;
    
        var routeHandler = _app.MapGet($"{apiBaseRoute}/{_baseRoute}", async (
                IMediator mediator,
                [FromQuery] string sort,
                [FromQuery] int offset,
                [FromQuery] int pageSize,
                [AsParameters] GetAllQueryParams queryParams) => // Novo parâmetro
            {
                try
                {
                    // Construir o dicionário manualmente
                    var filters = new Dictionary<string, object>();
                    sort = sort.Pascalize();
                    foreach (var prop in queryParams.GetType().GetProperties())
                    {
                        var value = prop.GetValue(queryParams);
                        if (value != null)
                            continue;
                        filters.Add(prop.Name, value);
                        
                    }

                    // Criar a query genérica
                    var queryType = typeof(GetAllQuery<>).MakeGenericType(entityType);
                    var query = Activator.CreateInstance(queryType);

                    // Definir propriedades
                    ReflectionExtensions.SetProperty(query, "Filters", filters);
                    ReflectionExtensions.SetProperty(query, "Sort", sort);
                    ReflectionExtensions.SetProperty(query, "Offset", offset);
                    ReflectionExtensions.SetProperty(query, "PageSize", pageSize);

                    // Executar a query
                    var result = await mediator.Send(query);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return HandleException(ex);
                }
            })
            .WithName($"GetAll{entityName}s")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(500);
    }

    private void GenerateGetByIdEndpoint(string entityName)
    {
        var entityType = _crudRoute.EntityType;
        var routeHandler = _app.MapGet($"{apiBaseRoute}/{_baseRoute}/{{id}}", async (
                string id,
                IMediator mediator) =>
            {
                try
                {
                    var keyProperty = GetKeyProperty(entityType);
                    if (keyProperty == null) throw new InvalidOperationException("Key property not found");

                    // Criar query genérica
                    var queryType = typeof(GetByIdQuery<>).MakeGenericType(entityType);
                    var query = Activator.CreateInstance(queryType);

                    object convertedId = ConvertKeyPropertyId(keyProperty, id);
                    
                    ReflectionExtensions.SetProperty(query, "Id", convertedId);

                    var result = await mediator.Send(query);
                    if(result is null)
                        return Results.NotFound();
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return HandleException(ex);
                }
            })
            .WithName($"Get{entityName}")
            .Produces(200)
            .Produces(404)
            .ProducesProblem(400)
            .ProducesProblem(500);
    }

    private void GenerateCreateEndpoint(string entityName)
    {
        var entityType = _crudRoute.EntityType;
        var commandType = typeof(CreateCommand<>).MakeGenericType(entityType);

        var routeHandler = _app.MapPost($"{apiBaseRoute}/{_baseRoute}", async (
                IMediator mediator,
                [FromBody] dynamic request) =>
            {
                try
                {
                    var command = Activator.CreateInstance(commandType);
                    ReflectionExtensions.CopyProperties(request, command);
                    Dictionary<string, object> props  = JsonSerializer.Deserialize<Dictionary<string, object>>(request);
                    props.Remove("id");
                    props.Remove("ID");
                    props.Remove("Id");
                    ReflectionExtensions.SetProperty(command, "Properties", props);
                    await mediator.Send(command);
                    return Results.Created();
                }
                catch (Exception ex)
                {
                    return HandleException(ex);
                }
            })
            .WithName($"Create{entityName}")
            .Produces(201)
            .ProducesProblem(400)
            .ProducesProblem(500);

        if (_disableAntiforgery) routeHandler.DisableAntiforgery();
    }
    
    
    private PropertyInfo GetKeyProperty(Type entityType)
    {
        // Busca a propriedade chave por convenção
        return entityType.GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) 
                                 || p.Name.Equals($"{entityType.Name}Id", StringComparison.OrdinalIgnoreCase));
    }

    private IResult HandleException(Exception ex)
    {
        return ex switch
        {
            ValidationException => Results.Problem(ex.Message, statusCode: 400),
            InvalidOperationException => Results.Problem(ex.Message, statusCode: 400),
            _ => Results.Problem("Internal error", statusCode: 500)
        };
    }
    
    private void ConfigureSwaggerSchemaForCommand(RouteHandlerBuilder routeHandler, Type commandType)
    {
    }

    private string GeneratePropertyDescription(PropertyInfo prop)
    {
        // Lógica personalizada para geração de descrição
        return $"Description for {prop.Name}";
    }

    private IOpenApiAny GenerateExampleValue(PropertyInfo prop)
    {
        return prop.PropertyType switch
        {
            Type t when t == typeof(string) => new OpenApiString("Example Value"),
            Type t when t == typeof(int) => new OpenApiInteger(42),
            Type t when t == typeof(DateTime) => new OpenApiDateTime(DateTime.Now),
            _ => null
        };
    }

    private async Task ValidateRequest(object command)
    {
        var validator = new DynamicValidator();
        validator.ConfigureRules(_crudRoute.ValidationRules);
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        if (_crudRoute.CustomValidation != null)
        {
            await _crudRoute.CustomValidation(command);
        }
    }

    private void ApplyTypeConstraints(OpenApiSchema propSchema, Type propertyType)
    {
        // Aplicar restrições baseadas no tipo
        switch (Type.GetTypeCode(propertyType))
        {
            case TypeCode.String:
                propSchema.MinLength = 1;
                propSchema.MaxLength = 255;
                break;

            case TypeCode.Int32:
                propSchema.Minimum = 0;
                propSchema.Maximum = 1000;
                break;
        }
    }

// Método de extensão útil para conversão de nome
}

// Classe auxiliar para capturar parâmetros dinâmicos