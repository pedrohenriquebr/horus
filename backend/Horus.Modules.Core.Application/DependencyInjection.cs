using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.TelegramBot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Horus.Modules.Core.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISystemInstructionBuilder, SystemInstructionBuilder>();
        services.AddScoped<IMemoryProcessor, MemoryProcessor>();
        services.AddTelegramBotServices();

        return services;
    }
}