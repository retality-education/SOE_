using SOE.Models;

namespace SOE.Repositories
{
    public interface IMessageRepository
    {
        Task AddMessageAsync(Message message);
        Task<List<ChatMessage>> GetChatHistoryAsync(string chatId, int offset, int limit);
        Task<bool> CanUserSendToChatAsync(string userId, string chatId);
    }
}
