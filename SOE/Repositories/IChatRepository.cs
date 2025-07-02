namespace SOE.Repositories
{
    public interface IChatRepository
    {
        Task CreateChatAsync(string chatId, string chatName, string creatorId);
        Task AddUserToChatAsync(string chatId, string userId);
        Task<bool> ChatExistsAsync(string chatId);
    }
}
