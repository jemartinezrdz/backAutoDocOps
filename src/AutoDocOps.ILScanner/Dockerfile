# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/AutoDocOps.ILScanner/AutoDocOps.ILScanner.csproj", "src/AutoDocOps.ILScanner/"]

RUN dotnet restore "src/AutoDocOps.ILScanner/AutoDocOps.ILScanner.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/AutoDocOps.ILScanner"
RUN dotnet build "AutoDocOps.ILScanner.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AutoDocOps.ILScanner.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r autodocops && useradd -r -g autodocops autodocops

# Install security updates
RUN apt-get update && apt-get upgrade -y && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R autodocops:autodocops /app
USER autodocops

# Health check for gRPC service
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Expose gRPC port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "AutoDocOps.ILScanner.dll"]

