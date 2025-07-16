# HealthController

The `HealthController` provides HTTP endpoints for monitoring the operational status of the application. It exposes lightweight health checks (`Health`, `Live`) and detailed diagnostics (`HealthDetailed`, `Ready`, `Info`) to support liveness probes, readiness checks, and general observability. The controller is designed for integration with monitoring tools, load balancers, and orchestration platforms (e.g., Kubernetes).

## API

### `HealthController`
The constructor for the `HealthController` class. Initializes a new instance with default values for `Name`, `Healthy`, and `Message`.

### `IActionResult Health()`
Returns a basic health status response.

**Purpose**:
Indicates whether the application is running. Intended for frequent, low-overhead checks (e.g., load balancer pings).

**Return Value**:
- `OkObjectResult` with a `200 OK` status if `Healthy` is `true`.
- `ObjectResult` with a `503 Service Unavailable` status if `Healthy` is `false`.

**Throws**:
- None.

---

### `Task<IActionResult> HealthDetailed()`
Returns a detailed health report, including asynchronous checks (e.g., database connectivity, external service dependencies).

**Purpose**:
Provides comprehensive diagnostics for troubleshooting or deeper monitoring. May perform I/O-bound operations.

**Return Value**:
- `OkObjectResult` with a `200 OK` status and a JSON payload containing detailed health metrics if all checks pass.
- `ObjectResult` with a `503 Service Unavailable` status and error details if any check fails.

**Throws**:
- May throw exceptions from underlying asynchronous operations (e.g., `HttpRequestException`, `SqlException`). Exceptions are caught and returned as `503` responses.

---

### `Task<IActionResult> Ready()`
Determines whether the application is ready to serve traffic (e.g., after startup or during maintenance).

**Purpose**:
Used by orchestration platforms to control traffic routing (e.g., Kubernetes readiness probes). May include checks for critical dependencies.

**Return Value**:
- `OkObjectResult` with a `200 OK` status if the application is ready.
- `ObjectResult` with a `503 Service Unavailable` status if the application is not ready.

**Throws**:
- May throw exceptions from asynchronous readiness checks. Exceptions are caught and returned as `503` responses.

---

### `IActionResult Live()`
Returns a minimal liveness confirmation.

**Purpose**:
Indicates whether the application process is running. Intended for frequent, low-latency checks (e.g., Kubernetes liveness probes).

**Return Value**:
- `OkResult` with a `200 OK` status if the application is alive.
- `StatusCodeResult` with a `503 Service Unavailable` status if the application is not alive.

**Throws**:
- None.

---

### `IActionResult Info()`
Returns metadata about the application (e.g., version, environment).

**Purpose**:
Provides static information for observability tools or debugging.

**Return Value**:
- `OkObjectResult` with a `200 OK` status and a JSON payload containing application metadata.

**Throws**:
- None.

---

### `string Name`
Gets or sets the name of the application or service.

**Purpose**:
Used in health reports to identify the service instance.

**Default Value**:
- `null` (should be set during initialization).

---

### `bool Healthy`
Gets or sets the overall health status of the application.

**Purpose**:
Determines the response of lightweight health endpoints (`Health`, `Live`). May be updated by background checks or external signals.

**Default Value**:
- `false` (should be set to `true` once initialization completes).

---

### `string Message`
Gets or sets a descriptive message about the application's health status.

**Purpose**:
Provides human-readable context for health reports (e.g., "Database connection failed").

**Default Value**:
- `null`.
