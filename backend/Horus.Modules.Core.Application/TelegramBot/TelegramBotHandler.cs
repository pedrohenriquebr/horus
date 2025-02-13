using System.Diagnostics;
using System.Text;
using Horus.Modules.Core.Application.Services;
using Horus.Modules.Core.Application.Usecases.GenerateText;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace Horus.Modules.Core.Application.TelegramBot;

public class TelegramBotHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly string _botToken;
    private readonly ILogger<TelegramBotHandler> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private ILlmProvider _llmService;
    private IMediator _mediator;

    public TelegramBotHandler(ILogger<TelegramBotHandler> logger,
        IServiceScopeFactory serviceScopeFactory, ITelegramBotClient botClient)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _botClient = botClient;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        _mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        _llmService = scope.ServiceProvider.GetRequiredService<ILlmProvider>();
        if (update.Message is not { } message)
            return;

        var chatId = message.Chat.Id;

        try
        {
            await HandleMessageAsync(botClient, update, cancellationToken);
        }
        catch (Exception ex)
        {
            await botClient.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken: cancellationToken);
            await botClient.SendTextMessageAsync(chatId, $"Error: {ex.Message}", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var message = update.Message!;
        var chatId = message.Chat.Id;

        if (message.Text is { } messageText)
        {
            _logger.LogInformation("Received a '{messageText}' message in chat {chatId}.", messageText, chatId);
            await _botClient.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken: cancellationToken);
            await HandleTextMessage(messageText, chatId, update, cancellationToken);
        }
        else if (message.Photo != null)
        {
            _logger.LogInformation("Received a photo in chat {chatId}.", chatId);
            await HandlePhotoMessage(message.Photo, message.Caption, chatId, update, cancellationToken);
        }
        else if (message.Voice != null)
        {
            _logger.LogInformation("Received a voice message in chat {chatId}.", chatId);
            await HandleVoiceMessage(message.Voice, chatId, update, cancellationToken);
        }
    }

    private async Task HandleTextMessage(string messageText, long chatId, Update update,
        CancellationToken cancellationToken)
    {
        if (messageText.StartsWith("/"))
        {
            await HandleCommand(messageText, chatId, cancellationToken);
            return;
        }

        Debug.Assert(update.Message != null, "update.Message != null");
        var user = update.Message.From;
        var userInfo = new Dictionary<string, string>
        {
            ["id"] = user.Id.ToString(),
            ["username"] = user.Username,
            ["first_name"] = user.FirstName,
            ["last_name"] = user.LastName,
            ["language_code"] = user.LanguageCode
        };

        var response = await _mediator.Send(new GenerateTextQuery
        {
            Prompt = messageText,
            UserInfo = userInfo
        });

        await _botClient.SendChatActionAsync(chatId, ChatAction.Typing, cancellationToken: cancellationToken);
        await _botClient.SendTextMessageAsync(chatId, EscapeMarkdownV2SpecialChars(response.Text), null,
            ParseMode.MarkdownV2, cancellationToken: cancellationToken);
    }

    private async Task HandlePhotoMessage(PhotoSize[] photos, string? caption, long chatId, Update update,
        CancellationToken cancellationToken)
    {
        var photo = photos.Last();
        var fileId = photo.FileId;
        var file = await _botClient.GetFileAsync(fileId, cancellationToken);
        var filePath = file.FilePath;

        var tempImagePath = Path.GetTempFileName();
        var extension = Path.GetExtension(filePath);
        tempImagePath = tempImagePath.Replace(".tmp", extension);
        await using (var fileStream = new FileStream(tempImagePath, FileMode.Create))
        {
            await _botClient.DownloadFileAsync(filePath, fileStream, cancellationToken);
        }

        // rename file


        try
        {
            var user = update.Message.From;
            var userInfo = new Dictionary<string, string>
            {
                ["id"] = user.Id.ToString(),
                ["username"] = user.Username,
                ["first_name"] = user.FirstName,
                ["last_name"] = user.LastName,
                ["language_code"] = user.LanguageCode
            };

            var response = await _mediator.Send(new GenerateTextQuery
            {
                Prompt = caption ?? "Analise esta imagem",
                ImagePath = tempImagePath,
                UserInfo = userInfo
            });

            await _botClient.SendTextMessageAsync(chatId, EscapeMarkdownV2SpecialChars(response.Text), null,
                ParseMode.MarkdownV2, cancellationToken: cancellationToken);
        }
        finally
        {
            File.Delete(tempImagePath);
        }
    }

    private async Task HandleVoiceMessage(Voice voice, long chatId, Update update, CancellationToken cancellationToken)
    {
        var fileId = voice.FileId;
        var file = await _botClient.GetFileAsync(fileId, cancellationToken);
        var filePath = file.FilePath;

        var tempAudioPath = Path.GetTempFileName();
        var extension = Path.GetExtension(filePath);
        tempAudioPath = tempAudioPath.Replace(".tmp", extension);
        await using (var fileStream = new FileStream(tempAudioPath, FileMode.Create))
        {
            await _botClient.DownloadFileAsync(filePath, fileStream, cancellationToken);
        }

        try
        {
            var user = update.Message.From;
            var userInfo = new Dictionary<string, string>
            {
                ["id"] = user.Id.ToString(),
                ["username"] = user.Username,
                ["first_name"] = user.FirstName,
                ["last_name"] = user.LastName,
                ["language_code"] = user.LanguageCode
            };

            var response = await _mediator.Send(new GenerateTextQuery
            {
                Prompt = "Transcreva e responda",
                AudioPath = tempAudioPath,
                UserInfo = userInfo
            });

            await _botClient.SendTextMessageAsync(chatId, EscapeMarkdownV2SpecialChars(response.Text), null,
                ParseMode.MarkdownV2, cancellationToken: cancellationToken);
        }
        finally
        {
            File.Delete(tempAudioPath);
        }
    }

    private async Task HandleCommand(string messageText, long chatId, CancellationToken cancellationToken)
    {
        switch (messageText)
        {
            case "/start":
                await _botClient.SendTextMessageAsync(chatId, "Welcome to the Horus Bot! How can I assist you today?",
                    cancellationToken: cancellationToken);
                break;
            case "/help":
                await _botClient.SendTextMessageAsync(chatId,
                    "Here are some commands you can use:\n/start - Start the bot\n/help - Show this help message",
                    cancellationToken: cancellationToken);
                break;
            default:
                await _botClient.SendTextMessageAsync(chatId,
                    "Unknown command. Type /help for a list of available commands.",
                    cancellationToken: cancellationToken);
                break;
        }
    }


    public string EscapeMarkdownV2SpecialChars(string text)
    {
        // Todos os caracteres especiais que precisam ser escapados conforme a documentação oficial
        var specialChars = new HashSet<char>
        {
            '_', '*', '[', ']', '(', ')', '~', '>',
            '#', '+', '-', '=', '|', '{', '}', '.', '!', '\\'
        };

        var escapedText = new StringBuilder(text.Length * 2); // Pré-alocação para melhor performance

        foreach (var c in text)
        {
            if (specialChars.Contains(c)) escapedText.Append('\\');

            escapedText.Append(c);
        }

        return escapedText.ToString();
    }

    public async Task<object> HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error while handling telegram bot update");

        return Task.CompletedTask;
    }
}