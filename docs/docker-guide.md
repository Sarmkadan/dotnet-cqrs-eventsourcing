# Docker Guide

## Introduction

This guide provides a comprehensive overview of using Docker with dotnet-cqrs-eventsourcing.

## Quick Start

1. Clone the repository: `git clone https://github.com/sarmkadan/dotnet-cqrs-eventsourcing.git`
2. Change into the directory: `cd dotnet-cqrs-eventsourcing`
3. Build the Docker image: `docker build -t dotnet-cqrs-eventsourcing .`
4. Run the Docker container: `docker run -p 8080:8080 dotnet-cqrs-eventsourcing`

## Docker Compose

The `docker-compose.yml` file provides a complete local development and testing environment.

### Services

1. **api** - Main CQRS application on port 8080
2. **sqlserver** - SQL Server 2022 for event store on port 1433
3. **redis** - Redis 7-alpine for caching on port 6379
4. **adminer** - Database UI on port 8081 (optional)

### Key Features

- Network isolation: `cqrs-network` bridge driver
- Health checks: All services include liveness probes
- Dependency ordering: Services wait for dependencies to be healthy
- Auto-restart: `unless-stopped` policy for reliability
- Volume management: Named volumes for data persistence

## Environment Variables

Update these in your deployment (docker-compose.yml or container orchestration):

```bash
# Event Store Connection String
EventStoreConnection=Server=sqlserver:1433;Database=CqrsEventStore;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true

# Redis Connection
RedisConnection=redis:6379

# Logging Level
Logging__LogLevel__Default=Information
```

**Security Warning:** Change `YourPassword123!` in production. Use secrets management (Kubernetes Secrets, HashiCorp Vault, etc.).

## Production Deployment Checklist

1. Update Docker image tags to match your registry
2. Configure environment variables for your production environment
3. Use a secrets manager for sensitive data (e.g., database passwords)
4. Implement monitoring and logging for your containers
5. Use a container orchestration tool (e.g., Kubernetes) for scalability and reliability

## Troubleshooting

### Docker Compose Issues

**SQL Server fails to start:**
- Check available disk space (MSSQL requires 2GB+)
- Verify TCP 1433 is not in use: `netstat -an | grep 1433`
- Review logs: `docker-compose logs sqlserver`

**API cannot connect to database:**
- Verify `EventStoreConnection` string matches service names
- Check network: `docker network ls` and `docker network inspect cqrs-network`
- Test connectivity: `docker-compose exec api ping sqlserver`

**Redis connection timeout:**
- Ensure Redis is healthy: `docker-compose ps`
- Check Redis logs: `docker-compose logs redis`
- Verify `RedisConnection` uses service hostname, not localhost

### Logging & Debugging

**Enable verbose logging:**
```yaml
environment:
  - Logging__LogLevel__Default=Debug
```

**View real-time logs:**
```bash
docker-compose logs -f api
```

**Exec into container for diagnostics:**
```bash
docker-compose exec api /bin/bash
```

## Conclusion

This guide provides a comprehensive overview of using Docker with dotnet-cqrs-eventsourcing. By following the steps outlined in this guide, you can create a complete local development and testing environment using Docker Compose.

## What's Next

- [ ] Deploy to Kubernetes (update docker-compose to Helm charts)
- [ ] Add distributed tracing (OpenTelemetry integration)
- [ ] Event versioning enhancements (schema evolution support)
- [ ] Performance benchmarks for v2.0.0 release
