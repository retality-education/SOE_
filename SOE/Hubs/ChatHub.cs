using Microsoft.AspNetCore.SignalR;
using SOE.Models;
using SOE.Repositories;
using SOE.Services;
using Microsoft.AspNetCore.Authorization;

namespace SOE.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatRepository _chatRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly IChatMoodCache _moodCache;
        private readonly EmotionService _moodService;

        public ChatHub(
            IChatRepository chatRepo,
            IMessageRepository messageRepo,
            IChatMoodCache moodCache,
            EmotionService moodService)
        {
            _chatRepo = chatRepo;
            _messageRepo = messageRepo;
            _moodCache = moodCache;
            _moodService = moodService;
        }

        // Создание нового чата
        public async Task<string> CreateChat(string chatName)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Требуется авторизация");
            var chatId = Guid.NewGuid().ToString();
            await _chatRepo.CreateChatAsync(chatId, chatName, Context.UserIdentifier);
            return chatId;
        }

        // Вступление в существующий чат
        public async Task JoinChat(string chatId)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Требуется авторизация");

            await _chatRepo.AddUserToChatAsync(chatId, Context.UserIdentifier);
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
            await Clients.Group(chatId).SendAsync("UserJoined", Context.UserIdentifier);
        }

        // Отправка сообщения
        public async Task<SendMessageResult> SendMessage(string chatId, string text)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Требуется авторизация");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                UserId = Context.UserIdentifier,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            await _messageRepo.AddMessageAsync(message);

            var analysis = await _moodService.AnalyzeTextAsync(text);
            await _moodCache.UpdateMoodAsync(chatId, analysis.Mood);

            await Clients.Group(chatId).SendAsync("ReceiveMessage", message);

            return new SendMessageResult
            {
                IsPositive = analysis.Mood == "joy",
                Hint = analysis.Hint
            };
        }

        // Загрузка истории сообщений
        public async Task<List<Message>> LoadHistory(string chatId, int offset, int limit)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Требуется авторизация");

            return await _messageRepo.GetChatHistoryAsync(chatId, offset, limit);
        }
    }
}

public class TestUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        return "meow";
    }
}
