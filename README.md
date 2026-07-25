# AjoibBot 🤖

Telegram-бот для магазина детской одежды + Admin REST API на C# / .NET 10

Бот ведёт диалог с покупателем через OpenAI (function calling): модель отвечает на
вопросы о товарах, ценах, размерах и наличии, читая каталог напрямую из БД через
инструменты `search_products` / `list_categories` — без выдумывания данных.

Бот также умеет оформлять заказ (`create_order`): собирает товары, количество, имя и
телефон покупателя, подтверждает сумму, списывает остаток на складе и уведомляет
продавца сообщением в Telegram. История заказов доступна продавцу через
`GET /api/orders` в Admin.Api.

## Стек технологий

- **Backend:** ASP.NET Core 10, C#
- **ORM:** Entity Framework Core + Dapper
- **LLM:** OpenAI (Chat Completions + function calling) — диалог бота с покупателем
- **База данных:** PostgreSQL
- **Авторизация:** JWT Bearer tokens
- **Документация:** Swagger / OpenAPI
- **Логирование:** Serilog
- **Контейнеризация:** Docker + docker-compose
- **Тесты:** xUnit + Moq

## Архитектура

```
AjoibBot/
├── AjoibBot.API            # Telegram Bot (Worker Service) + диалог через OpenAI
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

Создай файл `.env` в корне проекта:

```
TELEGRAM_BOT_TOKEN=your_telegram_bot_token
TELEGRAM_ADMIN_CHAT_ID=chat_id_продавца
OPENAI_API_KEY=your_openai_api_key
```

`TELEGRAM_ADMIN_CHAT_ID` — chat_id продавца, куда бот присылает уведомления о новых
заказах. Чтобы узнать его: продавец пишет боту любое сообщение, затем в браузере
открой `https://api.telegram.org/bot<TELEGRAM_BOT_TOKEN>/getUpdates` и возьми
`message.chat.id` из ответа.

```bash
docker compose up
```

API будет доступен на `http://localhost:8080/swagger`

## Запуск локально

```bash
# Установи секреты Admin API
cd src/AjoibBot.Admin.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=postgres;Username=admin;Password="
dotnet user-secrets set "Jwt:Key" "your_secret_key_min_32_chars"
cd ../..

# Установи секреты Telegram-бота
cd src/AjoibBot.API
dotnet user-secrets set "Telegram:BotToken" "your_telegram_bot_token"
dotnet user-secrets set "Telegram:AdminChatId" "chat_id_продавца"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=postgres;Username=admin;Password="
dotnet user-secrets set "OpenAI:ApiKey" "your_openai_api_key"
cd ../..

# Запусти
dotnet run --project src/AjoibBot.Admin.Api
dotnet run --project src/AjoibBot.API
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
| GET | /api/orders | История заказов, оформленных через бота |

## Тесты

```bash
dotnet test src/AjoibBot.Tests
```