# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY AutoDocOps.sln ./
COPY src/AutoDocOps.Domain/AutoDocOps.Domain.csproj src/AutoDocOps.Domain/
COPY src/AutoDocOps.Application/AutoDocOps.Application.csproj src/AutoDocOps.Application/
COPY src/AutoDocOps.Infrastructure/AutoDocOps.Infrastructure.csproj src/AutoDocOps.Infrastructure/
COPY src/AutoDocOps.WebAPI/AutoDocOps.WebAPI.csproj src/AutoDocOps.WebAPI/
COPY tests/AutoDocOps.Tests/AutoDocOps.Tests.csproj tests/AutoDocOps.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/src/AutoDocOps.WebAPI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "AutoDocOps.WebAPI.dll"]

