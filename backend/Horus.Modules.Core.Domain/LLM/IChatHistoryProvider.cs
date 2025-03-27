using Horus.Modules.Core.Domain.Entities;
using Horus.Modules.Shared.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Horus.Modules.Core.Domain.LLM;

public interface IChatHistoryProvider
{
    Task StoreMessageAsync(string role, string content, Dictionary<string, string> userInfo, Dictionary<string, object>? additionalMetadata = null);
    Task<IEnumerable<ChatMessage>> GetHistoryAsync(Dictionary<string, string> userInfo, int limit = 50);

    Task ClearHistoryAsync(Dictionary<string, string> userInfo);
    Task StoreMessagesAsync(Dictionary<string, string> userInfo, List<ChatMessage> history);
}

public record ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> UserInfo { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class RedisChatHistoryProvider : IChatHistoryProvider
{
    private readonly string _keyTemplate;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHorusContext _horusContext;

    public RedisChatHistoryProvider(IConnectionMultiplexer redis, IOptions<RedisConfig> options, IHorusContext horusContext)
    {
        _redis = redis;
        _horusContext = horusContext;
        _keyTemplate = $"{options.Value.ChatHistoryKey}{options.Value.KeyDelimiter}{{0}}";
    }

    public async Task StoreMessageAsync(string role, string content, Dictionary<string, string> userInfo, Dictionary<string, object>? additionalMetadata = null)
    {
        var chatSessionId = userInfo.GetValueOrDefault("chatSessionId");
        if (string.IsNullOrEmpty(chatSessionId))
            return;
        ChatSession? chatSession = null;
        
        
        var key = BuildKey(userInfo);
        var chatMessage = new ChatMessage
        {
            Role = role,
            Content = content,
            UserInfo = userInfo,
            Timestamp = DateTime.UtcNow
        };
        await _redis.GetDatabase().ListRightPushAsync(new RedisKey(key), JsonConvert.SerializeObject(chatMessage));

        if (!string.IsNullOrEmpty(chatSessionId))
        {
            chatSession = await _horusContext.ChatSessions.FirstOrDefaultAsync(x => x.Id == Guid.Parse(chatSessionId))!;
            
            if(chatSession == null)
                throw new Exception("Chat session not found");
            
            var msg = chatSession?.AddNewMessage(role, content);
            
            if(additionalMetadata != null)
                msg?.SetMetadata(additionalMetadata);
        }
            

    }

    public async Task<IEnumerable<ChatMessage>> GetHistoryAsync(Dictionary<string, string> userInfo, int limit = 50)
    {
        if(!userInfo.ContainsKey("chatSessionId"))
            return new List<ChatMessage>();
        
        var key = BuildKey(userInfo);

        // Redis ListRangeAsync uses inclusive indexes, so -50 means the 50th last message, and -1 means the latest message.
        var messages = await _redis.GetDatabase().ListRangeAsync(key, -limit);

        // Deserialize each message and return
        return messages.Select(m => JsonConvert.DeserializeObject<ChatMessage>(m));
    }


    public async Task ClearHistoryAsync(Dictionary<string, string> userInfo)
    {
        if (!userInfo.ContainsKey("chatSessionId"))
            return;
        var key = BuildKey(userInfo);
        await _redis.GetDatabase().KeyDeleteAsync(key);
    }

    public async Task StoreMessagesAsync(Dictionary<string, string> userInfo, List<ChatMessage> history)
    {
        foreach (var message in history) await StoreMessageAsync(message.Role, message.Content, message.UserInfo);
    }

    private string BuildKey(Dictionary<string, string> userInfo)
    {
        return string.Format(_keyTemplate, userInfo["chatSessionId"]);
    }
}