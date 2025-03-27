using System.Reflection;
using System.Text.Json;
using FluentValidation;
using Horus.Modules.Core.Application.Common.Helpers;
using Horus.Modules.Core.Domain.Entities;
using Humanizer;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Common;
// Comando genérico para Update
public class UpdateCommand<TEntity> : IRequest
{
    public Guid Id { get; set; }
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

public class GenericUpdateCommandHandler<TEntity> : IRequestHandler<UpdateCommand<TEntity>>
    where TEntity : BaseEntity
{
    private readonly DbContext _dbContext;
    private readonly CrudGeneratorOptions _options;
    private readonly string setterMethodPrefix;

    public GenericUpdateCommandHandler(DbContext dbContext, CrudGeneratorOptions options)
    {
        _dbContext = dbContext;
        _options = options;
        this.setterMethodPrefix = _options.SetterMethodPrefix;
    }

    public async Task Handle(UpdateCommand<TEntity> request, CancellationToken cancellationToken)
    {
        var entity = await EntityHelper.FindEntityByKey<TEntity>(_dbContext, request.Id, cancellationToken);
        if (entity == null) throw new ValidationException("Entity not found");

        foreach (var prop in request.Properties)
        {
            var pascalName = prop.Key.Pascalize();
            var entityType = typeof(TEntity);

            // 1. Tentar encontrar o método setter
            string methodName = $"{setterMethodPrefix}{pascalName}";

            var setterMethod = entityType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { ReflectionExtensions.GetPropertyType(entityType, pascalName) },
                null);

            // 2. Se encontrar o método setter
            if (setterMethod != null)
            {
                var currentValue = entityType.GetProperty(pascalName)?.GetValue(entity);
                var newValue = ConvertValue(prop.Value, setterMethod.GetParameters()[0].ParameterType);

                // 3. Só atualiza se o valor for diferente
                if (!Equals(currentValue, newValue))
                {
                    setterMethod.Invoke(entity, new[] { newValue });
                }

                continue;
            }

            // 4. Fallback para propriedades com setters privados (se necessário)
            var entityProp = entityType.GetProperty(pascalName, BindingFlags.Public | BindingFlags.Instance);
            if (entityProp?.CanWrite == true)
            {
                var newValue = ConvertValue(prop.Value, entityProp.PropertyType);
                if (!Equals(entityProp.GetValue(entity), newValue))
                {
                    entityProp.SetValue(entity, newValue);
                }
            }
        }

        _dbContext.Set<TEntity>().Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    private object ConvertValue(object value, Type targetType)
    {
        if (value is JsonElement jsonElement)
        {
            return jsonElement.Deserialize(targetType)
                   ?? throw new InvalidOperationException($"Failed to convert value to {targetType.Name}");
        }

        return Convert.ChangeType(value, targetType);
    }
}