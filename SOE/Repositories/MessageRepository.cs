using Npgsql;
using SOE.Models;

namespace SOE.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly NpgsqlConnection _connection;

        public MessageRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task AddMessageAsync(Message message)
        {
            const string query = """
            INSERT INTO messages (id, chat_id, user_id, text, timestamp)
            VALUES (@id, @chatId, @userId, @text, @timestamp)
            """;

            await _connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("id", message.Id);
            cmd.Parameters.AddWithValue("chatId", message.ChatId);
            cmd.Parameters.AddWithValue("userId", message.UserId);
            cmd.Parameters.AddWithValue("text", message.Text);
            cmd.Parameters.AddWithValue("timestamp", DateTime.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Message>> GetChatHistoryAsync(string chatId, int offset, int limit)
        {
            const string query = """
            SELECT * FROM messages 
            WHERE chat_id = @chatId 
            ORDER BY timestamp DESC
            OFFSET @offset LIMIT @limit
            """;

            await _connection.OpenAsync();
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("chatId", chatId);
            cmd.Parameters.AddWithValue("offset", offset);
            cmd.Parameters.AddWithValue("limit", limit);

            var reader = await cmd.ExecuteReaderAsync();
            var messages = new List<Message>();

            while (await reader.ReadAsync())
            {
                messages.Add(new Message
                {
                    Id = reader.GetGuid(0),
                    ChatId = reader.GetString(1),
                    Text = reader.GetString(2),
                    Timestamp = reader.GetDateTime(3)
                });
            }

            return messages;
        }
    }
}
