# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Mentoragente.API/Mentoragente.API.csproj", "Mentoragente.API/"]
COPY ["Mentoragente.Application/Mentoragente.Application.csproj", "Mentoragente.Application/"]
COPY ["Mentoragente.Domain/Mentoragente.Domain.csproj", "Mentoragente.Domain/"]
COPY ["Mentoragente.Infrastructure/Mentoragente.Infrastructure.csproj", "Mentoragente.Infrastructure/"]

RUN dotnet restore "Mentoragente.API/Mentoragente.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Mentoragente.API"
RUN dotnet build "Mentoragente.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Mentoragente.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Render uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Mentoragente.API.dll"]

