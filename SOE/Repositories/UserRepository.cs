using Npgsql;
using SOE.Models;
using System.Security.Cryptography;
using System.Text;

namespace SOE.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly NpgsqlConnection _connection;

        public UserRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<User> RegisterUserAsync(string username, string email, string password)
        {
            const string query = """
                INSERT INTO users (id, username, email, password_hash)
                VALUES (@id, @username, @email, @passwordHash)
                RETURNING id, username, email, created_at;
                """;

            var userId = Guid.NewGuid().ToString();
            var passwordHash = HashPassword(password);

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);

            cmd.Parameters.AddWithValue("id", userId);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("passwordHash", passwordHash);

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new User
            {
                Id = reader.GetString(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }

        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            const string query = """
                SELECT id, username, email, password_hash, created_at
                FROM users
                WHERE username = @username;
            """;

            try
            {
                if (_connection.State == System.Data.ConnectionState.Closed)
                {
                    await _connection.OpenAsync();
                }
                await using var cmd = new NpgsqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("username", username);

                await using var reader = await cmd.ExecuteReaderAsync();

                // Если пользователь не найден
                if (!await reader.ReadAsync())
                    return null;

                // Проверяем, есть ли поле password_hash (индекс 3)
                if (reader.IsDBNull(3))
                    return null;

                var storedHash = reader.GetString(3);
                var inputHash = HashPassword(password);

                // Сравниваем хеши
                if (!string.Equals(storedHash, inputHash, StringComparison.Ordinal))
                    return null;

                return new User
                {
                    Id = reader.GetString(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(4)
                };
            }
            catch (Exception ex)
            {
                // Логирование ошибки (в реальном приложении)
                Console.WriteLine($"Ошибка аутентификации: {ex.Message}");
                return null;
            }
            finally
            {
                // Закрываем соединение, если оно было открыто
                if (_connection.State == System.Data.ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            const string query = """
                SELECT id, username, email, created_at
                FROM users
                WHERE id = @userId;
                """;

            if (_connection.State == System.Data.ConnectionState.Closed)
            {
                await _connection.OpenAsync();
            }
            await using var cmd = new NpgsqlCommand(query, _connection);
            cmd.Parameters.AddWithValue("userId", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new User
            {
                Id = reader.GetString(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
