using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SOE.Hubs;
using SOE.Models;
using SOE.Providers;
using SOE.Repositories;
using SOE.Services;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));
builder.Services.AddScoped<NpgsqlConnection>(_ =>   
    new NpgsqlConnection(builder.Configuration.GetConnectionString("Postgres")));
// Репозитории
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Сервисы
builder.Services.AddSingleton<IChatMoodCache, RedisChatMoodCache>();
builder.Services.AddScoped<EmotionService>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// Аутентификация JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (RegisterRequest request, IUserRepository userRepo) =>
{
    try
    {
        var user = await userRepo.RegisterUserAsync(
            request.Username,
            request.Email,
            request.Password);

        return Results.Ok(user);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapPost("/api/auth/login", async (LoginRequest request, IUserRepository userRepo, IConfiguration config) =>
{
    var user = await userRepo.AuthenticateUserAsync(request.Username, request.Password);
    if (user is null)
        return Results.Unauthorized();

    var token = GenerateJwtToken(user, config);
    return Results.Ok(new AuthResponse { Token = token, User = user });
});

app.MapHub<ChatHub>("/chatHub");
app.MapHub<MoodAnalysisHub>("/moodHub");
app.MapGet("/healthy", async (NpgsqlConnection db, IConnectionMultiplexer redis) =>
{
    try
    {
        // ¿¿¿¿¿¿¿¿ ¿¿¿¿¿¿¿¿¿¿¿ ¿ PostgreSQL
        await db.OpenAsync();
        await db.CloseAsync();

        // ¿¿¿¿¿¿¿¿ ¿¿¿¿¿¿¿¿¿¿¿ ¿ Redis
        var redisDb = redis.GetDatabase();
        await redisDb.PingAsync();

        return Results.Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

string GenerateJwtToken(User user, IConfiguration config)
{
    var securityKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var credentials = new SigningCredentials(
        securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim("user_id", user.Id), // Для SignalR
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],
        audience: config["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddMinutes(
            Convert.ToDouble(config["Jwt:ExpiryInMinutes"])),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Модели запросов
record RegisterRequest(string Username, string Email, string Password);
record LoginRequest(string Username, string Password);
