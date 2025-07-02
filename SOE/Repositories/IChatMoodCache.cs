namespace SOE.Repositories
{
    public interface IChatMoodCache
    {
        /// <summary>
        /// Обновляет настроение чата в Redis.
        /// </summary>
        Task UpdateMoodAsync(string chatId, string mood);

        /// <summary>
        /// Возвращает текущее настроение чата.
        /// </summary>
        Task<string?> GetCurrentMoodAsync(string chatId);
    }
}
