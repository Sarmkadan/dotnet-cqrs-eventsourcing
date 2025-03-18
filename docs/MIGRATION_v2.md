# Migration Guide: v1.0.0 to v2.0.0

This guide covers upgrading from dotnet-cqrs-eventsourcing v1.0.0 to v2.0.0.

## Overview

Version 2.0.0 introduces production-grade Docker support with multi-stage builds, comprehensive docker-compose orchestration, and improved deployment documentation. The core CQRS and Event Sourcing API remains stable and backward compatible.

## Breaking Changes

**None.** The public API is fully backward compatible. Upgrades are non-breaking.

## What's New

### Docker Support

#### Multi-Stage Dockerfile
The Dockerfile now uses three-stage builds for optimization:
- **builder stage:** SDK image for restore and build (net10.0)
- **publish stage:** Publishes Release configuration
- **runtime stage:** Lean runtime image (net10.0), excluding SDK

Benefits:
- Reduced image size (SDK removed from final layer)
- Faster deployment iterations (cached builder layer)
- HEALTHCHECK endpoint for container orchestration

#### Port Configuration
- Exposed port: **8080** (HTTP)
- Environment variable: `ASPNETCORE_URLS=http://+:8080`
- No changes required to application code

### Docker Compose Orchestration

The docker-compose.yml provides a complete local development and testing environment:

#### Services
1. **api** - Main CQRS application on port 8080
2. **sqlserver** - SQL Server 2022 for event store on port 1433
3. **redis** - Redis 7-alpine for caching on port 6379
4. **adminer** - Database UI on port 8081 (optional)

#### Key Features
- Network isolation: `cqrs-network` bridge driver
- Health checks: All services include liveness probes
- Dependency ordering: Services wait for dependencies to be healthy
- Auto-restart: `unless-stopped` policy for reliability
- Volume management: Named volumes for data persistence

### Deployment Changes

#### Environment Variables
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

## Upgrade Steps

### For Docker/Compose Users

1. **Update the image build:**
   ```bash
   docker pull mcr.microsoft.com/dotnet/sdk:10.0
   docker pull mcr.microsoft.com/dotnet/runtime:10.0
   ```

2. **Rebuild application image:**
   ```bash
   docker build -t your-registry/cqrs-api:2.0.0 .
   ```

3. **Update docker-compose.yml** (if using custom compose file):
   - Ensure `ASPNETCORE_URLS` includes `http://+:8080`
   - Add `healthcheck` for the api service
   - Update dependency conditions if needed

4. **Restart services:**
   ```bash
   docker-compose down
   docker-compose up -d
   ```

5. **Verify health:**
   ```bash
   docker-compose ps
   docker logs dotnet-cqrs-api
   ```

### For Non-Docker Deployments

1. **Update NuGet package** to 2.0.0:
   ```bash
   dotnet add package Zaiets.dotnet.cqrs.eventsourcing --version 2.0.0
   ```

2. **No code changes required** - Public API is stable.

3. **Optional:** Add health check endpoint if using load balancers:
   ```csharp
   app.MapHealthChecks("/health");
   ```

4. **Optional:** Use ASPNETCORE_URLS in your deployment scripts:
   ```bash
   export ASPNETCORE_URLS="http://+:8080"
   dotnet DotNetCqrsEventSourcing.dll
   ```

## Dependency Updates

No mandatory dependency updates. All package versions remain compatible with v1.0.0.

Optional updates available:
- Microsoft.Extensions.* packages: v9.0.0 (already included)
- .NET runtime: Update from net10.0 base images if using older SDK versions

## Verification Checklist

After upgrading, verify:

- [ ] Docker image builds successfully: `docker build -t test .`
- [ ] docker-compose stack starts: `docker-compose up -d`
- [ ] API is healthy: `curl http://localhost:8080/health` (if health endpoint exists)
- [ ] Database connection works: Check logs for SQL Server connection errors
- [ ] Redis cache operational: `docker exec dotnet-cqrs-redis redis-cli ping` returns PONG
- [ ] Event sourcing works: Run integration tests
- [ ] Projections update: Verify read model consistency
- [ ] Snapshots save/restore: Verify aggregate snapshots work

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

## Support

- GitHub Issues: https://github.com/sarmkadan/dotnet-cqrs-eventsourcing/issues
- Documentation: See `docs/` folder for architecture, API reference, and FAQ
- Pull requests welcome!

## What's Next

- [ ] Deploy to Kubernetes (update docker-compose to Helm charts)
- [ ] Add distributed tracing (OpenTelemetry integration)
- [ ] Event versioning enhancements (schema evolution support)
- [ ] Performance benchmarks for v2.0.0 release
