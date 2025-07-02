using StackExchange.Redis;

namespace SOE.Repositories
{
    public class RedisChatMoodCache : IChatMoodCache
    {
        private readonly IDatabase _redis;

        public RedisChatMoodCache(IConnectionMultiplexer redisConnection)
        {
            _redis = redisConnection.GetDatabase();
        }
        public async Task UpdateMoodAsync(string chatId, string mood)
        {
            await _redis.StringSetAsync(
                $"chat:{chatId}:mood",
                mood,
                TimeSpan.FromHours(1)
            );
        }

        public async Task<string?> GetCurrentMoodAsync(string chatId)
        {
            return await _redis.StringGetAsync($"chat:{chatId}:mood");
        }
    }
}
