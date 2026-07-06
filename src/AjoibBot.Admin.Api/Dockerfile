# ================================
# Этап 1 — сборка (build stage)
# ================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файлы проектов и восстанавливаем зависимости
# Делаем это отдельно от кода — Docker кэширует этот слой
# и не будет перекачивать NuGet пакеты при каждой сборке
COPY src/AjoibBot.Application/AjoibBot.Application.csproj src/AjoibBot.Application/
COPY src/AjoibBot.Infrastructure/AjoibBot.Infrastructure.csproj src/AjoibBot.Infrastructure/
COPY src/AjoibBot.Admin.Api/AjoibBot.Admin.Api.csproj src/AjoibBot.Admin.Api/

RUN dotnet restore src/AjoibBot.Admin.Api/AjoibBot.Admin.Api.csproj

# Копируем весь остальной код и публикуем
COPY . .
RUN dotnet publish src/AjoibBot.Admin.Api/AjoibBot.Admin.Api.csproj \
    -c Release -o /app/publish

# ================================
# Этап 2 — runtime (финальный образ)
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Копируем только собранный результат из этапа 1
COPY --from=build /app/publish .

# Порт на котором слушает приложение
EXPOSE 8080

# Команда запуска
ENTRYPOINT ["dotnet", "AjoibBot.Admin.Api.dll"]