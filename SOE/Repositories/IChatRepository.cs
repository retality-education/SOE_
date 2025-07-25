﻿using SOE.Models;

namespace SOE.Repositories
{
    public interface IChatRepository
    {
        Task CreateChatAsync(string chatId, string chatName, string creatorId);
        Task AddUserToChatAsync(string chatId, string userId);
        Task<bool> ChatExistsAsync(string chatId);
        Task<List<ChatSummary>> GetChatsForUserAsync(string userId);
        Task<bool> IsUserInChatAsync(string userId, string chatId);
        Task RemoveUserFromChatAsync(string chatId, string userId);

    }
}
