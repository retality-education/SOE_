using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Npgsql;
using SOE.Repositories;
using SOE.Services;
using SOE.Hubs;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);


// SignalR
builder.Services.AddSignalR();

// PostgreSQL
builder.Services.AddScoped<NpgsqlConnection>(_ =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("Postgres")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));

// Репозитории и сервисы
builder.Services.AddSingleton<IChatMoodCache, RedisChatMoodCache>();
builder.Services.AddScoped<EmotionService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

builder.Services.AddSingleton<IUserIdProvider, TestUserIdProvider>();

var app = builder.Build();
app.MapHub<ChatHub>("/chatHub");
app.MapHub<MoodAnalysisHub>("/moodHub");
app.Run();