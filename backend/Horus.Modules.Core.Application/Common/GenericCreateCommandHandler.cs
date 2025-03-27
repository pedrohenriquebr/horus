using System.Reflection;
using System.Text.Json;
using Horus.Modules.Core.Application.Common.Helpers;
using Horus.Modules.Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Common;
// Comando genérico para Create
public class CreateCommand<TEntity> : IRequest
{
    public Guid Id { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
public class GenericCreateCommandHandler<TEntity> : IRequestHandler<CreateCommand<TEntity>>
    where TEntity : BaseEntity
{
    private readonly DbContext _dbContext;

    public GenericCreateCommandHandler(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CreateCommand<TEntity> request, CancellationToken cancellationToken)
    {
        try
        {
            var isExist = await EntityHelper.FindEntityByKey<TEntity>(_dbContext, request.Id, cancellationToken);
            
            if(isExist != null)
                throw new Exception("Entity already exists");
            
            var entityType = typeof(TEntity);
            var keyProperty = EntityHelper.GetKeyProperty<TEntity>(_dbContext);

            // 1. Encontrar o construtor público mais adequado
            var constructor = entityType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
                throw new InvalidOperationException($"No public constructor found for {entityType.Name}");

            // 2. Preparar os parâmetros do construtor
            var parameters = constructor.GetParameters();
            object[] constructorArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramName = param.Name!;

                // 3. Mapear camelCase -> PascalCase
                if (paramName.Equals(keyProperty?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // Caso especial para a propriedade chave
                    constructorArgs[i] = Convert.ChangeType(request.Id, param.ParameterType);
                }
                else
                {
                    // Buscar o valor no dicionário usando camelCase
                    var camelCaseName = paramName.ToCamelCase();
                    if (request.Properties.TryGetValue(camelCaseName, out var value))
                    {
                        constructorArgs[i] = ConvertJsonElement(value, param.ParameterType);
                    }
                    else
                    {
                        throw new ArgumentException($"Missing required property: {camelCaseName}");
                    }
                }
            }

            // 4. Criar a instância usando o construtor
            var entity = (TEntity)constructor.Invoke(constructorArgs);


            await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating entity {typeof(TEntity).Name}", ex);
        }
    }


    private static object ConvertJsonElement(object value, Type targetType)
    {
        
        if (value is null)
            return null;
        
        if (value is JsonElement jsonElement)
        {
            return jsonElement.Deserialize(targetType) ??
                   throw new InvalidOperationException($"Failed to convert JSON value to {targetType.Name}");
        }

        return Convert.ChangeType(value, targetType);
    }
}