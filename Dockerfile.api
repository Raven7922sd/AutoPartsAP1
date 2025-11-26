# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["AutoParts.Api/AutoParts.Api.csproj", "AutoParts.Api/"]
COPY ["AutoParts.Shared/AutoParts.Shared.csproj", "AutoParts.Shared/"]
RUN dotnet restore "AutoParts.Api/AutoParts.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/AutoParts.Api"
RUN dotnet build "AutoParts.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AutoParts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoParts.Api.dll"]
