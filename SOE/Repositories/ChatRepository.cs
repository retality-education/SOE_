using Npgsql;
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
    }
}
