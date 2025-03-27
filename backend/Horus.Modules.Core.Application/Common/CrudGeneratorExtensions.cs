using System.Linq.Expressions;
using System.Reflection;
using System.Threading.RateLimiting;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Horus.Modules.Core.Application.Common;

public static class CrudGeneratorExtensions
{
    public static WebApplication UseCrudGenerator(this WebApplication app)
    {
        var services = app.Services;
        var crudGenerator = services.GetRequiredService<CrudGeneratorService>();
        var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        crudGenerator.SetDbContext(dbContext);
        crudGenerator.RegisterEndpoints(app);

        return app;
    }


    public static IServiceCollection AddCrudGenerator(this IServiceCollection services,
        Action<CrudGeneratorOptions> handler)
    {
        var options = new CrudGeneratorOptions();
        handler.Invoke(options);

        if (string.IsNullOrEmpty(options.BaseRoute))
            throw new Exception("Specify the base route for crud generator");

        RegisterHandlers(options, services);

        services.AddSingleton(options);

        services.AddSingleton<CrudGeneratorService>(provider =>
            new CrudGeneratorService(options, services, provider.GetRequiredService<ILogger<CrudGeneratorService>>()));

        return services;
    }

    public static void RegisterHandlers(CrudGeneratorOptions _options, IServiceCollection _services)
    {
        foreach (var crudRoute in _options.CrudRoutes)
        {
            var entityType = crudRoute.EntityType;

            if (crudRoute.EnableCreate)
            {
                var handlerType = typeof(GenericCreateCommandHandler<>).MakeGenericType(entityType);
                var commandType = typeof(CreateCommand<>).MakeGenericType(entityType);
                _services.AddTransient(typeof(IRequestHandler<>).MakeGenericType(commandType), handlerType);
            }

            if (crudRoute.EnableUpdate)
            {
                var handlerType = typeof(GenericUpdateCommandHandler<>).MakeGenericType(entityType);
                var commandType = typeof(UpdateCommand<>).MakeGenericType(entityType);
                _services.AddTransient(typeof(IRequestHandler<>).MakeGenericType(commandType), handlerType);
            }

            if (crudRoute.EnableDelete)
            {
                var handlerType = typeof(GenericDeleteCommandHandler<>).MakeGenericType(entityType);
                var commandType = typeof(DeleteCommand<>).MakeGenericType(entityType);
                _services.AddTransient(typeof(IRequestHandler<>).MakeGenericType(commandType), handlerType);
            }

            if (crudRoute.EnableRead)
            {
                var handlerType = typeof(GenericGetByIdQueryHandler<>).MakeGenericType(entityType);
                var queryType = typeof(GetByIdQuery<>).MakeGenericType(entityType);
                _services.AddTransient(typeof(IRequestHandler<,>).MakeGenericType(queryType, typeof(object)),
                    handlerType);
            }

            if (crudRoute.EnableGetAll)
            {
                var handlerType = typeof(GenericGetAllQueryHandler<>).MakeGenericType(entityType);
                var queryType = typeof(GetAllQuery<>).MakeGenericType(entityType);
                _services.AddTransient(typeof(IRequestHandler<,>).MakeGenericType(queryType, typeof(object)),
                    handlerType);
            }
        }
    }

    public static string GetPropertyName<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression)
    {
        // Tratar diferentes tipos de expressões
        switch (expression.Body)
        {
            // Expressão de membro direto (propriedade simples)
            case MemberExpression memberExpression:
                return memberExpression.Member.Name;

            // Conversões de tipo ou outras operações unárias
            case UnaryExpression unaryExpression
                when unaryExpression.Operand is MemberExpression innerMemberExpression:
                return innerMemberExpression.Member.Name;

            // Caso não consiga extrair
            default:
                throw new ArgumentException(
                    "A expressão deve ser uma expressão de propriedade válida.",
                    nameof(expression)
                );
        }
    }

    public static void AddRoutes(this List<CrudRoute> collection, params CrudRoute[] routes)
    {
        collection.AddRange(routes);
    }
}