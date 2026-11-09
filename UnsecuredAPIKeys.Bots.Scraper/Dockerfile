# Build stage - build the Scraper and its project references
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["UnsecuredAPIKeys.Data/UnsecuredAPIKeys.Data.csproj", "UnsecuredAPIKeys.Data/"]
COPY ["UnsecuredAPIKeys.Providers/UnsecuredAPIKeys.Providers.csproj", "UnsecuredAPIKeys.Providers/"]
COPY ["UnsecuredAPIKeys.Bots.Scraper/UnsecuredAPIKeys.Bots.Scraper.csproj", "UnsecuredAPIKeys.Bots.Scraper/"]
RUN dotnet restore "UnsecuredAPIKeys.Bots.Scraper/UnsecuredAPIKeys.Bots.Scraper.csproj"

COPY UnsecuredAPIKeys.Data/ UnsecuredAPIKeys.Data/
COPY UnsecuredAPIKeys.Providers/ UnsecuredAPIKeys.Providers/
COPY UnsecuredAPIKeys.Bots.Scraper/ UnsecuredAPIKeys.Bots.Scraper/
WORKDIR /src/UnsecuredAPIKeys.Bots.Scraper
RUN dotnet publish "UnsecuredAPIKeys.Bots.Scraper.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "UnsecuredAPIKeys.Bots.Scraper.dll"]
