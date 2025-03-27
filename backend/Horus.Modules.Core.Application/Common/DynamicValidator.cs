using FluentValidation;

namespace Horus.Modules.Core.Application.Common;

public class DynamicValidator : AbstractValidator<object>
{
    private List<ValidationRule> _rules;

    // Remova o construtor com parâmetro
    public DynamicValidator()
    {
    }

    public void ConfigureRules(List<ValidationRule> rules)
    {
        _rules = rules;
        foreach (var rule in _rules)
        {
            var propertyRule = RuleFor(x => x.GetType().GetProperty(rule.PropertyName).GetValue(x));
            rule.Rule(propertyRule);
        }
    }

    public async Task ValidateAsync(object instance, List<ValidationRule> rules, CancellationToken cancellationToken = default)
    {
        ConfigureRules(rules);
        var result = await ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}