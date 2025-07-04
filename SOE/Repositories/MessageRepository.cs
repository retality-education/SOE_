using Npgsql;
using SOE.Models;

namespace SOE.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly IChatRepository _chatRepository;

        public MessageRepository(NpgsqlConnection connection, IChatRepository chatRepository)
        {
            _connection = connection;
            _chatRepository = chatRepository;
        }

        public async Task AddMessageAsync(Message message)
        {
            // Проверяем, что пользователь состоит в чате
            if (!await _chatRepository.IsUserInChatAsync(message.UserId, message.ChatId))
                throw new InvalidOperationException("Пользователь не состоит в этом чате");

            const string query = """
                INSERT INTO messages (id, chat_id, user_id, text)
                VALUES (@id, @chatId, @userId, @text);
                """;

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("id", message.Id);
            cmd.Parameters.AddWithValue("chatId", message.ChatId);
            cmd.Parameters.AddWithValue("userId", message.UserId);
            cmd.Parameters.AddWithValue("text", message.Text);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(string chatId, int offset, int limit)
        {
            const string query = """
                SELECT m.id, m.chat_id, m.user_id, u.username, m.text, m.timestamp
                FROM messages m
                JOIN users u ON m.user_id = u.id
                WHERE m.chat_id = @chatId
                ORDER BY m.timestamp DESC
                OFFSET @offset LIMIT @limit;
                """;

            var messages = new List<ChatMessage>();

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("chatId", chatId);
            cmd.Parameters.AddWithValue("offset", offset);
            cmd.Parameters.AddWithValue("limit", limit);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                messages.Add(new ChatMessage
                {
                    Id = reader.GetGuid(0),
                    ChatId = reader.GetString(1),
                    UserId = reader.GetString(2),
                    Username = reader.GetString(3),
                    Text = reader.GetString(4),
                    Timestamp = reader.GetDateTime(5)
                });
            }

            return messages;
        }

        public async Task<bool> CanUserSendToChatAsync(string userId, string chatId)
        {
            return await _chatRepository.IsUserInChatAsync(userId, chatId);
        }
    }
}
