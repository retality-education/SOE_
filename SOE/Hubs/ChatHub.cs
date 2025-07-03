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
        public async Task<bool> JoinChat(string chatId)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Требуется авторизация");

            try
            {
                await _chatRepo.AddUserToChatAsync(chatId, Context.UserIdentifier);

                await Groups.AddToGroupAsync(Context.ConnectionId, chatId);

                var recentMessages = await _messageRepo.GetChatHistoryAsync(chatId, 0, 10);
                var combinedText = string.Join(" ", recentMessages.Select(m => m.Text));
                var mood = await _moodService.AnalyzeTextAsync(combinedText);

                await Clients.Caller.SendAsync("ChatMood", new
                {
                    Mood = mood.Mood,
                    BackgroundColor = mood.BackgroundColor,
                    TextColor = mood.TextColor,
                    SuggestedPhrases = mood.SuggestedPhrases
                });


                await Clients.Group(chatId).SendAsync("UserJoined", Context.UserIdentifier);
            }
            catch (InvalidOperationException e) { 
                Console.WriteLine("Такого чата нету");
                return false;
            }
            return true;

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
            await Clients.Group(chatId).SendAsync("ChatUpdated", new ChatUpdate
            {
                ChatId = chatId,
                LastMessage = message.Text,
                Timestamp = message.Timestamp
            });

            return new SendMessageResult
            {
                IsPositive = analysis.Mood == "joy",
                Hint = analysis.Hint
            };
        }

        // Загрузка истории сообщений
        public async Task<List<ChatMessage>> LoadHistory(string chatId, int offset, int limit)
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Требуется авторизация");

            return await _messageRepo.GetChatHistoryAsync(chatId, offset, limit);
        }
        // Получение всех чатов пользователя
        public async Task<List<ChatSummary>> GetMyChats()
        {
            if (string.IsNullOrEmpty(Context.UserIdentifier))
                throw new HubException("Требуется авторизация");

            return await _chatRepo.GetChatsForUserAsync(Context.UserIdentifier);
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
