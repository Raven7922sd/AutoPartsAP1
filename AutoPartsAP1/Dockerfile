# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["AutoPartsAP1/AutoPartsAP1.csproj", "AutoPartsAP1/"]
COPY ["AutoParts.Shared/AutoParts.Shared.csproj", "AutoParts.Shared/"]
RUN dotnet restore "AutoPartsAP1/AutoPartsAP1.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/AutoPartsAP1"
RUN dotnet build "AutoPartsAP1.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AutoPartsAP1.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AutoPartsAP1.dll"]
