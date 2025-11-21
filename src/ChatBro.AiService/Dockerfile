# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ChatBro.sln", "./"]
COPY ["src/ChatBro.AiService/ChatBro.AiService.csproj", "src/ChatBro.AiService/"]
COPY ["src/ChatBro.RestaurantsService.KernelFunction/ChatBro.RestaurantsService.KernelFunction.csproj", "src/ChatBro.RestaurantsService.KernelFunction/"]
COPY ["src/ChatBro.RestaurantsService.Model/ChatBro.RestaurantsService.Model.csproj", "src/ChatBro.RestaurantsService.Model/"]
COPY ["src/ChatBro.ServiceDefaults/ChatBro.ServiceDefaults.csproj", "src/ChatBro.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/ChatBro.AiService/ChatBro.AiService.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/ChatBro.AiService"
RUN dotnet publish "ChatBro.AiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatBro.AiService.dll"]
