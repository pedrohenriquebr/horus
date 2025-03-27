using System.Reflection;
using Horus.Modules.Core.Application.Common.Helpers;
using Humanizer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Horus.Modules.Core.Application.Common;

public class CrudGeneratorService
{
    private readonly CrudGeneratorOptions _options;
    private Dictionary<Type, List<Type>> entityTypesToCommandTypes;
    private readonly Dictionary<string, Type> patternToTypes;
    private readonly Dictionary<string, Dictionary<OperationType, List<string>>> patternToNameOfSchema;
    private readonly IServiceCollection _services; // Adicionado para registrar serviços
    private readonly ILogger<CrudGeneratorService> _logger;
    private DbContext _dbContext;
    private Dictionary<string,string> patternToNameOfEntityType ;

    public CrudGeneratorService(CrudGeneratorOptions options, IServiceCollection services, ILogger<CrudGeneratorService> logger)
    {
        _options = options;
        _services = services;
        _logger = logger;
        patternToNameOfSchema = new Dictionary<string, Dictionary<OperationType, List<string>>>();
        patternToTypes = new Dictionary<string, Type>();
        entityTypesToCommandTypes = new Dictionary<Type, List<Type>>();
        patternToNameOfEntityType = new Dictionary<string, string>();
    }


    public void SetDbContext(DbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public void RegisterEndpoints(WebApplication app)
    {
        // RegisterHandlers(); // Registrar os handlers genéricos primeiro

        this.patternToNameOfEntityType = this._options.CrudRoutes
                .ToDictionary(x => $"{this._options.BaseRoute}/{x.BaseRoute}",
                x => x.EntityType.Name)
                .Concat(this._options.CrudRoutes
                    .ToDictionary(x=> $"{this._options.BaseRoute}/{x.BaseRoute}/{{id}}",
                        x => x.EntityType.Name))
                .ToDictionary(x => x.Key, x => x.Value);
        
        
        _options.CrudRoutes.ForEach(crudRoute =>
        {
            var entityType = crudRoute.EntityType;
            var entityName = entityType.Name;
            var pattern = $"{_options.BaseRoute}/{crudRoute.BaseRoute}";

            // Gerar tipos genéricos dinamicamente
            var createCommandType = typeof(CreateCommand<>).MakeGenericType(entityType);
            var updateCommandType = typeof(UpdateCommand<>).MakeGenericType(entityType);
            var deleteCommandType = typeof(DeleteCommand<>).MakeGenericType(entityType);
            var getByIdQueryType = typeof(GetByIdQuery<>).MakeGenericType(entityType);
            var getAllQueryType = typeof(GetAllQuery<>).MakeGenericType(entityType);

            // Registrar esquemas
            entityTypesToCommandTypes.TryAdd(entityType, new List<Type>
            {
                createCommandType,
                updateCommandType,
                deleteCommandType,
                getByIdQueryType,
                getAllQueryType
            });

            if (crudRoute.EnableCreate)
            {
                if (!patternToNameOfSchema.ContainsKey(pattern))
                    patternToNameOfSchema.Add(pattern, new Dictionary<OperationType, List<string>>());
                patternToNameOfSchema[pattern].Add(OperationType.Post, [$"Create{entityName}Command"]);
            }

            if (crudRoute.EnableUpdate)
            {
                if (!patternToNameOfSchema.ContainsKey(pattern + "/{id}"))
                    patternToNameOfSchema.Add(pattern + "/{id}", new Dictionary<OperationType, List<string>>());
                patternToNameOfSchema[pattern + "/{id}"].Add(OperationType.Put, [$"Update{entityName}Command"]);
            }

            


            ValidateEntity(crudRoute);
            var endpointBuilder = new CrudEndpointBuilder(app, _options.BaseRoute, crudRoute.BaseRoute, crudRoute)
                .ToggleCreate(crudRoute.EnableCreate)
                .ToggleUpdate(crudRoute.EnableUpdate)
                .ToggleDelete(crudRoute.EnableDelete)
                .ToggleGetAll(crudRoute.EnableGetAll)
                .ToggleRead(crudRoute.EnableRead)
                .ToggleAntiforgery(!crudRoute.EnableAntiforgery);

            endpointBuilder.Register();
        });
    }


    private (List<string>, List<string>) GetPropsForCreateAndUpdateCommands(Type entityType)
    {
        var keyProperty = EntityHelper.GetKeyProperty(_dbContext, entityType);
        // 1. Encontrar o construtor público mais adequado
        var constructor = entityType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        List<string> forUpdateList = new List<string>();
        List<string> forCreateList = new List<string>();

        if (constructor == null)
            throw new InvalidOperationException($"No public constructor found for entity: {entityType.Name}");

        var parameters = constructor.GetParameters();
        object[] constructorArgs = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = param.Name!;
            var pascalName = paramName.Pascalize();

            if (paramName.Equals(keyProperty?.Name, StringComparison.OrdinalIgnoreCase))
            {
                forCreateList.Add(pascalName.Camelize());
                continue;
            }

            forCreateList.Add(pascalName.Camelize());
            forUpdateList.Add(pascalName.Camelize());
        }

        return (forCreateList, forUpdateList);
    }

    private void ValidateEntity(CrudRoute crudRoute)
    {
     
        var entityType = crudRoute.EntityType;
     
       
        
        var keyProperty = EntityHelper.GetKeyProperty(_dbContext, entityType);
        // 1. Encontrar o construtor público mais adequado
        var constructor = entityType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();


        if (constructor == null)
            throw new InvalidOperationException($"No public constructor found for entity: {entityType.Name}");

        
        if (!crudRoute.EnableUpdate)
        {
            _logger.LogDebug("Entity {Entity} does not have Update operation, skipping validation for setters methods...", crudRoute.EntityType.Name);
            return;
        }
        
        var parameters = constructor.GetParameters();
        object[] constructorArgs = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = param.Name!;
            var pascalName = paramName.Pascalize();

            if (paramName.Equals(keyProperty?.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            string methodName = $"{_options.SetterMethodPrefix}{pascalName}";

            var setterMethod = entityType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { ReflectionExtensions.GetPropertyType(entityType, pascalName) },
                null);

            if (setterMethod == null)
                throw new InvalidOperationException($"On the entity {entityType.FullName}:\n" +
                                                    $"Setter method not found for property '{paramName}', please create the following method '{methodName}'");
        }
    }


    public void RegisterSchemas(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var crudRoute in _options.CrudRoutes)
        {
            var entityType = crudRoute.EntityType;
            var entityName = entityType.Name;

            // 1. Gerar schema baseado na entidade
            var entitySchema = GenerateEntitySchema(entityType, context, true);

            // 2. Criar schemas customizados para os comandos
            CreateCommandSchemas(swaggerDoc, context, entityName, entitySchema, entityType);
            CreateQuerySchemas(swaggerDoc, entityName, entitySchema);
        }

        ApplySchemaMappings(swaggerDoc);
    }

    private OpenApiSchema GenerateEntitySchema(Type entityType, DocumentFilterContext context, bool forResponse)
    {
        var schema = context.SchemaGenerator.GenerateSchema(entityType, context.SchemaRepository);

        var members = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

        // Adicionar Metadata como objeto genérico
        schema.Properties["Metadata"] = new OpenApiSchema
        {
            Type = "object",
            AdditionalPropertiesAllowed = true,
            AdditionalProperties = new OpenApiSchema { Type = "object" }
        };

        foreach (var member in members)
        {
            schema.Properties.TryAdd(member.Name.Camelize(),
                context.SchemaGenerator.GenerateSchema(member.PropertyType, context.SchemaRepository));
        }

        return schema;
    }

    private void CreateCommandSchemas(OpenApiDocument swaggerDoc, DocumentFilterContext context, string entityName,
        OpenApiSchema entitySchema, Type entityType)
    {
        var (createList, updateList) = GetPropsForCreateAndUpdateCommands(entityType);
        // Schema para Create
        var createSchema = new OpenApiSchema
        {
            Type = "object",
            Properties =
                new Dictionary<string, OpenApiSchema>(entitySchema.Properties.Where(d => createList.Contains(d.Key)))
        };
        createSchema.Properties.Remove("Id"); // Id é gerado automaticamente
        swaggerDoc.Components.Schemas.Add($"Create{entityName}Command", createSchema);

        // Schema para Update
        var updateSchema = new OpenApiSchema
        {
            Type = "object",
            Properties =
                new Dictionary<string, OpenApiSchema>(entitySchema.Properties.Where(d => updateList.Contains(d.Key)))
        };
        swaggerDoc.Components.Schemas.Add($"Update{entityName}Command", updateSchema);
    }

    private void CreateQuerySchemas(OpenApiDocument swaggerDoc, string entityName, OpenApiSchema entitySchema)
    {
        // GetById Response
        var getByIdSchema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = entitySchema.Reference.Id
            }
        };
        swaggerDoc.Components.Schemas.Add($"Get{entityName}ByIdResponse", getByIdSchema);

        // GetAll Response
        var getAllSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["items"] = new OpenApiSchema
                {
                    Type = "array",
                    Items = getByIdSchema
                },
                ["total"] = new OpenApiSchema { Type = "integer" },
                ["page"] = new OpenApiSchema { Type = "integer" },
                ["totalPages"] = new OpenApiSchema { Type = "integer" },
                ["pageSize"] = new OpenApiSchema { Type = "integer" }
            }
        };
        swaggerDoc.Components.Schemas.Add($"GetAll{entityName}Response", getAllSchema);
    }

    private void ApplySchemaMappings(OpenApiDocument swaggerDoc)
    {
        var endpoints = swaggerDoc.Paths
            .Where(p => p.Key.Contains(this._options.BaseRoute))
            .ToList();

        foreach (var path in endpoints)
        {
            foreach (var operation in path.Value.Operations)
            {
                string? entityName = GetEntityNameFromPath(path.Key);
                
                if(entityName is null)
                    continue;

                switch (operation.Key)
                {
                    case OperationType.Get:
                        if (path.Key.Contains("{id}"))
                            MapResponse(operation.Value, $"Get{entityName}ByIdResponse");
                        else
                            MapResponse(operation.Value, $"GetAll{entityName}Response");
                        break;

                    case OperationType.Post:
                        MapRequest(operation.Value, $"Create{entityName}Command");
                        break;

                    case OperationType.Put:
                        MapRequest(operation.Value, $"Update{entityName}Command");
                        break;
                    //
                    case OperationType.Delete:
                        operation.Value.Responses["204"] = new OpenApiResponse { Description = "No Content" };
                        break;
                }
            }
        }
    }

    private void MapRequest(OpenApiOperation operation, string schemaName)
    {
        operation.RequestBody = new OpenApiRequestBody
        {
            Description = "Success",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = schemaName
                        }
                    }
                }
            }
        };
    }

    private void MapResponse(OpenApiOperation operation, string schemaName)
    {
        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Success",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                        { Reference = new OpenApiReference { Id = schemaName, Type = ReferenceType.Schema } }
                }
            }
        };
    }

    private string? GetEntityNameFromPath(string path)
    {
        // Exemplo: "/api/projects" → "Project"
        if(patternToNameOfEntityType.TryGetValue(path, out var entity))
            return entity;

        return null;
    }
}