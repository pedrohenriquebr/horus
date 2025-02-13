using Horus.Modules.Core.Domain.LLM;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Modules.Core.Domain;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddDomain(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IChatHistoryProvider, RedisChatHistoryProvider>();
        return builder;
    }
}