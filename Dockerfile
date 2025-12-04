# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY TextHumanizer.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Railway uses PORT env variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE 8080
ENTRYPOINT ["dotnet", "TextHumanizer.dll"]
