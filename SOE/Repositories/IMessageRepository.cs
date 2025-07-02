using SOE.Models;

namespace SOE.Repositories
{
    public interface IMessageRepository
    {
        /// <summary>
        /// Сохраняет сообщение в БД.
        /// </summary>
        Task AddMessageAsync(Message message);

        /// <summary>
        /// Загружает историю сообщений без анализа.
        /// </summary>
        Task<List<ChatMessage>> GetChatHistoryAsync(string chatId, int offset, int limit);
    }
}
