# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/AutoDocOps.WebAPI/AutoDocOps.WebAPI.csproj", "src/AutoDocOps.WebAPI/"]
COPY ["src/AutoDocOps.Application/AutoDocOps.Application.csproj", "src/AutoDocOps.Application/"]
COPY ["src/AutoDocOps.Domain/AutoDocOps.Domain.csproj", "src/AutoDocOps.Domain/"]
COPY ["src/AutoDocOps.Infrastructure/AutoDocOps.Infrastructure.csproj", "src/AutoDocOps.Infrastructure/"]

RUN dotnet restore "src/AutoDocOps.WebAPI/AutoDocOps.WebAPI.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/AutoDocOps.WebAPI"
RUN dotnet build "AutoDocOps.WebAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AutoDocOps.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks and security updates
RUN apt-get update && \
    apt-get install -y curl && \
    apt-get upgrade -y && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r autodocops && useradd -r -g autodocops autodocops

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R autodocops:autodocops /app
USER autodocops

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "AutoDocOps.WebAPI.dll"]

