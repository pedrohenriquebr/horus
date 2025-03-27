using System.Linq.Expressions;
using FluentValidation;
using Horus.Modules.Core.Domain.Entities;

namespace Horus.Modules.Core.Application.Common;

public class CrudRoute
{
    public Type EntityType { get; set; }
    public string BaseRoute { get; set; }
    public bool EnableCreate { get; set; } = true;
    public bool EnableRead { get; set; } = true;
    public bool EnableUpdate { get; set; } = true;
    public bool EnableDelete { get; set; } = true;
    public bool EnableGetAll { get; set; } = true;
    public bool EnableAntiforgery { get; set; } = true;
    
    // Novas propriedades para configurações dinâmicas
    public List<ValidationRule> ValidationRules { get; set; } = new List<ValidationRule>();
    
    public Func<object, Task> CustomValidation { get; set; }


    // Métodos fluentes para configurações
    public CrudRoute WithValidationRule<TEntity, TProperty>(
        Expression<Func<TEntity, TProperty>> expression, 
        Action<IRuleBuilderInitial<TEntity, TProperty>> rule)
    {
        // Conversão do nome da propriedade
        string propertyName = expression.GetPropertyName();

        // Wrapper para converter a regra de validação
        Action<IRuleBuilderInitial<object, object>> convertedRule = 
            (IRuleBuilderInitial<object, object> ruleBuilder) => 
            {
                // Converter o ruleBuilder para o tipo específico
                var typedRuleBuilder = ruleBuilder as IRuleBuilderInitial<TEntity, TProperty>;
            
                if (typedRuleBuilder != null)
                {
                    // Invocar a regra original com o tipo correto
                    rule(typedRuleBuilder);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Não foi possível converter o rule builder para o tipo esperado."
                    );
                }
            };

        ValidationRules.Add(new ValidationRule 
        { 
            PropertyName = propertyName, 
            Rule = convertedRule 
        });

        return this;
    }

    public CrudRoute WithCustomValidation(Func<object, Task> customValidation)
    {
        CustomValidation = customValidation;
        return this;
    }

    private CrudRoute()
    {
    }

    public static CrudRoute For<TEntity>(string baseRoute = null)
        where TEntity : BaseEntity
    {
        var @new = new CrudRoute();
        if (string.IsNullOrEmpty(baseRoute))
        {
            baseRoute = typeof(TEntity).Name.ToLowerInvariant();
        }

        @new.BaseRoute = baseRoute;
        @new.EntityType = typeof(TEntity);
        return @new;
    }

    public CrudRoute WithCreate(bool enable)
    {
        EnableCreate = enable;
        return this;
    }

    public CrudRoute WithRead(bool enable)
    {
        EnableRead = enable;
        return this;
    }

    public CrudRoute WithUpdate(bool enable)
    {
        EnableUpdate = enable;
        return this;
    }

    public CrudRoute WithDelete(bool enable)
    {
        EnableDelete = enable;
        return this;
    }

    public CrudRoute DisableAntiforgery()
    {
        EnableAntiforgery = false;
        return this;
    }

    public CrudRoute WithGetAll(bool enable)
    {
        EnableGetAll = enable;
        return this;
    }
}