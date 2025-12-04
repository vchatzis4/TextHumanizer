# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY TextHumanizer.UI.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - use Node to serve static files
FROM node:20-alpine
WORKDIR /app

# Install serve globally
RUN npm install -g serve

# Copy built files
COPY --from=build /app/publish/wwwroot .

# Railway sets PORT env variable
ENV PORT=8080
EXPOSE 8080

# Serve with single-page app mode (-s)
CMD ["sh", "-c", "serve -s -l $PORT"]
