using AjoibBot.Application.Interfaces;
using AjoibBot.Grpc.Services;
using AjoibBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new Exception("Connection string не найдена!");

builder.Services.AddDbContext<AjoibBotDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection(); // ← добавь это

var app = builder.Build();

app.MapGrpcService<GreeterService>();
app.MapGrpcService<ProductGrpcService>();

// Reflection для grpcurl
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService(); // ← добавь это
}

app.MapGet("/", () =>
    "gRPC сервер запущен. Используй gRPC клиент для подключения.");

app.Run();