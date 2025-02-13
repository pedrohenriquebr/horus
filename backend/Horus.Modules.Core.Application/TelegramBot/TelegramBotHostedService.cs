using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace Horus.Modules.Core.Application.TelegramBot;

public class TelegramBotHostedService : IHostedService
{
    private readonly ITelegramBotClient? _botClient;
    private readonly TelegramBotHandler _botHandler;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotHostedService> _logger;
    private CancellationTokenSource? _cts;

    public TelegramBotHostedService(
        ILogger<TelegramBotHostedService> logger,
        ITelegramBotClient botClient,
        TelegramBotHandler botHandler,
        IConfiguration configuration)
    {
        _logger = logger;
        _botClient = botClient;
        _botHandler = botHandler;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                _cts = new CancellationTokenSource();

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = Array.Empty<UpdateType>(),
                    ThrowPendingUpdates = true
                };

                _botClient.StartReceiving(
                    async (botClient, update, ct) =>
                        await _botHandler.HandleUpdateAsync(botClient, update, ct),
                    async (botClient, exception, ct) =>
                    {
                        _logger.LogError(exception, "Error while handling telegram bot update");
                        _logger.LogError(exception, "Error starting Telegram bot. Will retry in 10 seconds...");
                        await Task.Delay(10000, cancellationToken); // Wait before retrying
                        await StartAsync(cancellationToken);
                    },
                    receiverOptions,
                    _cts.Token
                );

                _logger.LogInformation("Telegram bot started successfully");
                break; // Exit loop if successful
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting Telegram bot. Will retry in 10 seconds...");
                await Task.Delay(10000, cancellationToken); // Wait before retrying
            }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _logger.LogInformation("Telegram bot stopped successfully");
        return Task.CompletedTask;
    }
}