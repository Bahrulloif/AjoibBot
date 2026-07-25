# ================================
# Этап 1 — сборка (build stage)
# ================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/AjoibBot.Application/AjoibBot.Application.csproj src/AjoibBot.Application/
COPY src/AjoibBot.Infrastructure/AjoibBot.Infrastructure.csproj src/AjoibBot.Infrastructure/
COPY src/AjoibBot.API/AjoibBot.API.csproj src/AjoibBot.API/

RUN dotnet restore src/AjoibBot.API/AjoibBot.API.csproj

COPY . .
RUN dotnet publish src/AjoibBot.API/AjoibBot.API.csproj \
    -c Release -o /app/publish

# ================================
# Этап 2 — runtime (финальный образ)
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AjoibBot.API.dll"]
