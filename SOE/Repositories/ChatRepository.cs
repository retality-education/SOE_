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
                VALUES (@id, @name, @creatorId);
                """;


            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);

            cmd.Parameters.AddWithValue("id", chatId);
            cmd.Parameters.AddWithValue("name", chatName);
            cmd.Parameters.AddWithValue("creatorId", creatorId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task AddUserToChatAsync(string chatId, string userId)
        {
            const string checkQuery = """
                SELECT 1 FROM chats WHERE id = @chatId;
                """;

            const string insertQuery = """
                INSERT INTO chat_users (chat_id, user_id) VALUES (@chatId, @userId) ON CONFLICT DO NOTHING;
                """;

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }

            // Проверяем наличие чата
            await using (var checkCmd = new NpgsqlCommand(checkQuery, _connection))
            {
                checkCmd.Parameters.AddWithValue("chatId", chatId);
                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists is null)
                {
                    throw new InvalidOperationException($"Чат с id {chatId} не существует.");
                }
            }

            // Добавляем пользователя в чат
            await using (var insertCmd = new NpgsqlCommand(insertQuery, _connection))
            {
                insertCmd.Parameters.AddWithValue("chatId", chatId);
                insertCmd.Parameters.AddWithValue("userId", userId);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> ChatExistsAsync(string chatId)
        {
            const string query = """
                SELECT EXISTS(SELECT 1 FROM chats WHERE id = @chatId);
                """;

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("chatId", chatId);
            return (bool)await cmd.ExecuteScalarAsync();
        }

        public async Task<List<ChatSummary>> GetChatsForUserAsync(string userId)
        {
            const string query = """
                SELECT c.id, c.name, 
                       m.text AS last_message,
                       m.timestamp AS last_message_time
                FROM chats c
                JOIN chat_users cu ON c.id = cu.chat_id
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

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("userId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new ChatSummary
                {
                    ChatId = reader.GetString(0),
                    ChatName = reader.GetString(1),
                    LastMessagePreview = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LastMessageTime = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3)
                });
            }

            return result;
        }

        public async Task<bool> IsUserInChatAsync(string userId, string chatId)
        {
            const string query = """
                SELECT EXISTS(
                    SELECT 1 FROM chat_users 
                    WHERE chat_id = @chatId AND user_id = @userId
                );
                """;

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("chatId", chatId);
            cmd.Parameters.AddWithValue("userId", userId);
            return (bool)await cmd.ExecuteScalarAsync();
        }
        public async Task RemoveUserFromChatAsync(string chatId, string userId)
        {
            const string checkQuery = """
        SELECT 1 FROM chat_users 
        WHERE chat_id = @chatId AND user_id = @userId;
        """;

            const string deleteQuery = """
        DELETE FROM chat_users 
        WHERE chat_id = @chatId AND user_id = @userId;
        """;

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }

            // Проверяем, состоит ли пользователь в чате
            await using (var checkCmd = new NpgsqlCommand(checkQuery, _connection))
            {
                checkCmd.Parameters.AddWithValue("chatId", chatId);
                checkCmd.Parameters.AddWithValue("userId", userId);

                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new InvalidOperationException(
                        $"Пользователь {userId} не состоит в чате {chatId} или чат не существует");
                }
            }

            // Удаляем пользователя из чата
            await using (var deleteCmd = new NpgsqlCommand(deleteQuery, _connection))
            {
                deleteCmd.Parameters.AddWithValue("chatId", chatId);
                deleteCmd.Parameters.AddWithValue("userId", userId);
                await deleteCmd.ExecuteNonQueryAsync();
            }
        }
    }

}
