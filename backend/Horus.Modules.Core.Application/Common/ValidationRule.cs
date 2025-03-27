namespace Horus.Modules.Core.Application.Common;

public class ValidationRule
{
    public string PropertyName { get; set; }
    public Action<FluentValidation.IRuleBuilderInitial<object, object?>> Rule { get; set; }
}