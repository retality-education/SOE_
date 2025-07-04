using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SOE.Models;
using SOE.Repositories;
using SOE.Services;
using System.Text.RegularExpressions;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepo;
    private readonly IMessageRepository _messageRepo;
    private readonly IChatMoodCache _moodCache;
    private readonly EmotionService _moodService;
    private readonly IUserRepository _userRepo;

    public ChatHub(
        IChatRepository chatRepo,
        IMessageRepository messageRepo,
        IChatMoodCache moodCache,
        EmotionService moodService,
        IUserRepository userRepo)
    {
        _chatRepo = chatRepo;
        _messageRepo = messageRepo;
        _moodCache = moodCache;
        _moodService = moodService;
        _userRepo = userRepo;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var user = await _userRepo.GetUserByIdAsync(userId);
        await Clients.Caller.SendAsync("UserConnected", user.Username);
    }

    public async Task<string> CreateChat(string chatName)
    {
        var userId = Context.UserIdentifier;
        var chatId = Guid.NewGuid().ToString();
        await _chatRepo.CreateChatAsync(chatId, chatName, userId);
        await JoinChat(chatId); // Создатель автоматически вступает в чат
        return chatId;
    }

    public async Task<bool> JoinChat(string chatId)
    {
        var userId = Context.UserIdentifier;

        try
        {
            await _chatRepo.AddUserToChatAsync(chatId, userId);
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);

            var recentMessages = await _messageRepo.GetChatHistoryAsync(chatId, 0, 10);
            var combinedText = string.Join(" ", recentMessages.Select(m => m.Text));
            var mood = await _moodService.AnalyzeTextAsync(combinedText);

            await Clients.Caller.SendAsync("ChatMood", mood);
            await Clients.Group(chatId).SendAsync("UserJoined", Context.User?.Identity?.Name);

            return true;
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    public async Task LeaveChat(string chatId)
    {
        var userId = Context.UserIdentifier;

        // Удаляем пользователя из чата
        await _chatRepo.RemoveUserFromChatAsync(chatId, userId);

        // Удаляем соединение из группы
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);

        // Уведомляем других участников
        await Clients.Group(chatId).SendAsync("UserLeft", Context.User?.Identity?.Name);
    }

    public async Task<SendMessageResult> SendMessage(string chatId, string text)
    {
        var userId = Context.UserIdentifier;

        if (!await _chatRepo.IsUserInChatAsync(userId, chatId))
            throw new HubException("Нет доступа к чату");

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UserId = userId,
            Text = text,
            Timestamp = DateTime.UtcNow
        };

        await _messageRepo.AddMessageAsync(message);

        var analysis = await _moodService.AnalyzeTextAsync(text);
        await _moodCache.UpdateMoodAsync(chatId, analysis.Mood);

        await Clients.Group(chatId).SendAsync("ReceiveMessage", new ChatMessage
        {
            Id = message.Id,
            ChatId = message.ChatId,
            UserId = message.UserId,
            Text = message.Text,
            Timestamp = message.Timestamp,
            Username = Context.User?.Identity?.Name
        });

        return new SendMessageResult
        {
            IsPositive = analysis.Mood == "joy",
            Hint = analysis.Hint
        };
    }

    public async Task<List<ChatMessage>> LoadHistory(string chatId, int offset, int limit)
    {
        var userId = Context.UserIdentifier;

        if (!await _chatRepo.IsUserInChatAsync(userId, chatId))
            throw new HubException("Нет доступа к чату");

        return await _messageRepo.GetChatHistoryAsync(chatId, offset, limit);
    }

    public async Task<List<ChatSummary>> GetMyChats()
    {
        var userId = Context.UserIdentifier;
        return await _chatRepo.GetChatsForUserAsync(userId);
    }
}