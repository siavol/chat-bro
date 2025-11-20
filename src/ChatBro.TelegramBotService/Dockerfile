# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ChatBro.sln", "./"]
COPY ["src/ChatBro.TelegramBotService/ChatBro.TelegramBotService.csproj", "src/ChatBro.TelegramBotService/"]
COPY ["src/ChatBro.ServiceDefaults/ChatBro.ServiceDefaults.csproj", "src/ChatBro.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/ChatBro.TelegramBotService/ChatBro.TelegramBotService.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/ChatBro.TelegramBotService"
RUN dotnet publish "ChatBro.TelegramBotService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatBro.TelegramBotService.dll"]
