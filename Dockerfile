# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# Dockerfile for .NET CQRS + Event Sourcing Framework
# Multi-stage build optimized for production deployment
# ===========================================================================

# Build stage - uses Alpine for minimal size
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS builder
WORKDIR /src

# Copy project files and restore dependencies
COPY *.csproj ./
COPY ./.editorconfig ./
RUN dotnet restore --verbosity minimal

# Copy source code and build
COPY . .
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM builder AS publish
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime stage - uses Alpine for minimal size
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Configure ASP.NET Core environment
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Create non-root user for security
RUN adduser -D -u 1001 appuser

# Copy published application from build stage
COPY --from=publish /app/publish .

# Copy entry point script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/ready || exit 1

# Expose port
EXPOSE 8080

# Switch to non-root user
USER appuser

# Set entry point
ENTRYPOINT ["/entrypoint.sh"]
CMD ["DotNetCqrsEventSourcing"]
