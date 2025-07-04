using SOE.Models;

namespace SOE.Repositories
{
    public interface IUserRepository
    {
        Task<User> RegisterUserAsync(string username, string email, string password);
        Task<User?> AuthenticateUserAsync(string username, string password);
        Task<User?> GetUserByIdAsync(string userId);
    }
}
