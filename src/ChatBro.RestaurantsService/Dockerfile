# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ChatBro.sln", "./"]
COPY ["src/ChatBro.RestaurantsService/ChatBro.RestaurantsService.csproj", "src/ChatBro.RestaurantsService/"]
COPY ["src/ChatBro.RestaurantsService.Model/ChatBro.RestaurantsService.Model.csproj", "src/ChatBro.RestaurantsService.Model/"]
COPY ["src/ChatBro.ServiceDefaults/ChatBro.ServiceDefaults.csproj", "src/ChatBro.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/ChatBro.RestaurantsService/ChatBro.RestaurantsService.csproj"

# Copy all source files
COPY . .

# Build and publish
WORKDIR "/src/src/ChatBro.RestaurantsService"
RUN dotnet publish "ChatBro.RestaurantsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatBro.RestaurantsService.dll"]
