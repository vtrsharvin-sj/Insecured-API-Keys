# Build stage - build the WebAPI and its project references
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["UnsecuredAPIKeys.Data/UnsecuredAPIKeys.Data.csproj", "UnsecuredAPIKeys.Data/"]
COPY ["UnsecuredAPIKeys.Providers/UnsecuredAPIKeys.Providers.csproj", "UnsecuredAPIKeys.Providers/"]
COPY ["UnsecuredAPIKeys.WebAPI/UnsecuredAPIKeys.WebAPI.csproj", "UnsecuredAPIKeys.WebAPI/"]
RUN dotnet restore "UnsecuredAPIKeys.WebAPI/UnsecuredAPIKeys.WebAPI.csproj"

COPY UnsecuredAPIKeys.Data/ UnsecuredAPIKeys.Data/
COPY UnsecuredAPIKeys.Providers/ UnsecuredAPIKeys.Providers/
COPY UnsecuredAPIKeys.WebAPI/ UnsecuredAPIKeys.WebAPI/
WORKDIR /src/UnsecuredAPIKeys.WebAPI
RUN dotnet publish "UnsecuredAPIKeys.WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Plain HTTP by default; put a reverse proxy in front for TLS (see docs/DEPLOYMENT_LINUX.md)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UnsecuredAPIKeys.WebAPI.dll"]
