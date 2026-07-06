# AjoibBot 🤖

Telegram-бот для магазина детской одежды + Admin REST API на C# / .NET 10

## Стек технологий

- **Backend:** ASP.NET Core 10, C#
- **ORM:** Entity Framework Core + Dapper
- **База данных:** PostgreSQL
- **Авторизация:** JWT Bearer tokens
- **Документация:** Swagger / OpenAPI
- **Логирование:** Serilog
- **Контейнеризация:** Docker + docker-compose
- **Тесты:** xUnit + Moq

## Архитектура

```
AjoibBot/
├── AjoibBot.API            # Telegram Bot (Worker Service)
├── AjoibBot.Admin.Api      # REST API + JWT + Swagger
├── AjoibBot.Application    # Сущности, интерфейсы, DTO
├── AjoibBot.Infrastructure # EF Core, Dapper, репозитории
└── AjoibBot.Tests          # Unit тесты (xUnit + Moq)
```

Clean Architecture — слои зависят только внутрь:
```
API → Application ← Infrastructure
```

## Запуск через Docker

```bash
docker compose up
```

API будет доступен на `http://localhost:8080/swagger`

## Запуск локально

```bash
# Установи секреты
cd src/AjoibBot.Admin.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=postgres;Username=admin;Password="
dotnet user-secrets set "Jwt:Key" "your_secret_key_min_32_chars"

# Запусти
cd ../..
dotnet run --project src/AjoibBot.Admin.Api
```

## API эндпоинты

| Метод | URL | Описание |
|-------|-----|----------|
| POST | /api/auth/login | Получить JWT токен |
| GET | /api/products | Список товаров |
| GET | /api/products/{id} | Товар по ID |
| POST | /api/products | Создать товар |
| PUT | /api/products/{id} | Обновить товар |
| DELETE | /api/products/{id} | Удалить товар |
| GET | /api/reports/categories/stats | Статистика по категориям |

## Тесты

```bash
dotnet test src/AjoibBot.Tests
```