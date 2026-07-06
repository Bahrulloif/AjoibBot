using AjoibBot.Infrastructure.Services;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

var token = builder.Configuration["Telegram:BotToken"] 
?? throw new Exception("Telegram bot token is not configured.");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

builder.Services.AddHostedService<BotPollingService>();

var app = builder.Build();
await app.RunAsync();
