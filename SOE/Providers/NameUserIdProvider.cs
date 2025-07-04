using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SOE.Providers
{
    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // 1. Пробуем взять ID из специального claim
            var userId = connection.User?.FindFirst("user_id")?.Value
                       ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            // 2. Проверяем валидность ID
            if (string.IsNullOrEmpty(userId) || userId == "qwe")
            {
                throw new HubException("Invalid user identification");
            }

            return userId;
        }
    }
}
