using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Horus.Modules.Core.Application.Common.Helpers;

public static class EntityHelper
{
    public static PropertyInfo GetKeyProperty<TEntity>(DbContext dbContext)
    {
        return GetKeyProperty(dbContext, typeof(TEntity));
    }
    
    public static PropertyInfo GetKeyProperty(DbContext dbContext, Type type)
    {
        var entityType = dbContext.Model.FindEntityType(type);
        if (entityType != null)
        {
            var key = entityType.FindPrimaryKey();
            if (key != null)
            {
                var keyProperty = key.Properties.FirstOrDefault()?.PropertyInfo;
                if (keyProperty != null)
                    return keyProperty;
            }
        }

        
        var idProperty = type.GetProperty("Id") ??
                         type.GetProperty("ID") ??
                         type.GetProperty("id") ??
                         type.GetProperty($"{type.Name}Id");

        return idProperty;
    }

    public static async Task<TEntity> FindEntityByKey<TEntity>(DbContext dbContext, object keyValue, CancellationToken cancellationToken)
        where TEntity : class
    {
        var keyProperty = GetKeyProperty<TEntity>(dbContext);
        if (keyProperty == null)
            throw new InvalidOperationException($"Key property not found for entity {typeof(TEntity).Name}");

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(parameter, keyProperty);
        var convertedValue = Convert.ChangeType(keyValue, keyProperty.PropertyType);
        var constant = Expression.Constant(convertedValue);
        var equality = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(equality, parameter);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        return await dbContext.Set<TEntity>().FirstOrDefaultAsync(lambda, cancellationTokenSource.Token);
    }
}