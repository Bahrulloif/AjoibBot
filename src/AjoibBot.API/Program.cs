using AjoibBot.Application.Interfaces;
using AjoibBot.Infrastructure.Data;
using AjoibBot.Infrastructure.Services;
using AjoibBot.Infrastructure.Services.OpenAi;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

var token = builder.Configuration["Telegram:BotToken"]
?? throw new Exception("Telegram bot token is not configured.");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Connection string не найдена!");

var openAiApiKey = builder.Configuration["OpenAI:ApiKey"]
    ?? throw new Exception("OpenAI API key is not configured.");
var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

builder.Services.AddDbContext<AjoibBotDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddSingleton(new ChatClient(model: openAiModel, apiKey: openAiApiKey));
builder.Services.AddSingleton<CatalogAssistantService>();

builder.Services.AddHostedService<BotPollingService>();

var app = builder.Build();
await app.RunAsync();
