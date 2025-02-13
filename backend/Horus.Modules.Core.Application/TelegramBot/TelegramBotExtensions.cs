using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

namespace Horus.Modules.Core.Application.TelegramBot;

public static class TelegramBotExtensions
{
    public static IServiceCollection AddTelegramBotServices(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramBotClient>(d =>
        {
            var configuration = d.GetService<IConfiguration>();
            return new TelegramBotClient(configuration?["Telegram:Token"] ??
                                         throw new ArgumentNullException("Telegram:Token must be configured"));
        });

        services.AddSingleton<TelegramBotHandler>();
        services.AddHostedService<TelegramBotHostedService>();

        return services;
    }
}