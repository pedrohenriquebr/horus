using System.Linq.Expressions;
using Horus.Modules.Core.Application.Abstractions.Messaging;
using MediatR;

namespace Horus.Modules.Core.Application.Common;

public class CrudGeneratorOptions
{
    public string BaseRoute { get; set; }
    public List<CrudRoute> CrudRoutes { get; set; } = new List<CrudRoute>();
    public string SetterMethodPrefix { get; set; } = "Set";
}
