using Npgsql;
using SOE.Models;
using System;
using System.Threading.Tasks;
namespace SOE.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly NpgsqlConnection _connection;

        public ChatRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task CreateChatAsync(string chatId, string chatName, string creatorId)
        {
            const string query = """
            INSERT INTO chats (id, name, created_by)
            VALUES (@id, @name, @creatorId)
            """;

            await _connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, _connection);
            
            cmd.Parameters.AddWithValue("id", chatId);
            cmd.Parameters.AddWithValue("name", chatName);
            cmd.Parameters.AddWithValue("creatorId", creatorId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddUserToChatAsync(string chatId, string userId)
        {
            const string query = """
            INSERT INTO chat_users (chat_id, user_id)
            VALUES (@chatId, @userId)
            ON CONFLICT DO NOTHING
            """;

            await _connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("chatId", chatId);
            cmd.Parameters.AddWithValue("userId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ChatExistsAsync(string chatId)
        {
            const string query = """
            SELECT EXISTS(SELECT 1 FROM chats WHERE id = @chatId)
            """;

            await _connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("chatId", chatId);
            return (bool)await cmd.ExecuteScalarAsync();
        }
        public async Task<List<ChatSummary>> GetChatsForUserAsync(string userId)
        {
            const string query = """
                SELECT c.id AS chat_id,
                       c.name AS chat_name,
                       m.text AS last_message,
                       m.timestamp AS last_timestamp
                FROM chats c
                INNER JOIN chat_users cu ON c.id = cu.chat_id
                LEFT JOIN LATERAL (
                    SELECT text, timestamp
                    FROM messages
                    WHERE chat_id = c.id
                    ORDER BY timestamp DESC
                    LIMIT 1
                ) m ON true
                WHERE cu.user_id = @userId
                ORDER BY m.timestamp DESC NULLS LAST
                """;

            var result = new List<ChatSummary>();

            await _connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("userId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var summary = new ChatSummary
                {
                    ChatId = reader.GetString(0),
                    ChatName = reader.GetString(1),
                    LastMessagePreview = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    LastMessageTime = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3)
                };

                result.Add(summary);
            }

            return result;
        }

    }
}
