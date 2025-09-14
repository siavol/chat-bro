# Quickstart Guide

This guide provides instructions on how to run the Personal AI Assistant.

## Prerequisites
- .NET 8 SDK
- Docker

## Configuration
1. Obtain a Telegram Bot Token from the BotFather on Telegram.
2. Configure the token in the application's settings file (`appsettings.json` or environment variables).

```json
{
  "BotConfiguration": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN"
  }
}
```

## Running the Application

### Using .NET CLI
```bash
dotnet run --project src/ChatBro.App/ChatBro.App.csproj
```

### Using Docker
```bash
docker build -t chat-bro .
docker run -d -p 8080:8080 --name chat-bro-container chat-bro
```
