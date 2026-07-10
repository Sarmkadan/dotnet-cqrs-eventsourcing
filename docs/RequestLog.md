# RequestLog

`RequestLog` is a data structure used within the `dotnet-cqrs-eventsourcing` project to capture and persist detailed information about HTTP requests and their corresponding responses. It serves as an audit trail for incoming requests, including metadata such as headers, body content, execution duration, and error details. This type is typically used in middleware or logging components to record request lifecycle events for debugging, monitoring, or compliance purposes.

## API

### `public string RequestId`
A unique identifier for the request. This value is generated per request and is used to correlate logs and traces across the system.

### `public string CorrelationId`
A unique identifier used to correlate multiple requests within the same logical operation or workflow. This value may be propagated across service boundaries to enable distributed tracing.

### `public DateTime Timestamp`
The date and time at which the request was received. This value is set automatically when the `RequestLog` instance is created.

### `public string Method`
The HTTP method of the request (e.g., `GET`, `POST`, `PUT`, `DELETE`).

### `public string Path`
The path component of the request URI, excluding the query string. Example: `/api/users`.

### `public string? QueryString`
The query string portion of the request URI, if present. This value is `null` if no query string exists. Example: `?id=123`.

### `public Dictionary<string, string> Headers`
A collection of key-value pairs representing the HTTP headers sent with the request. Header names are stored in lowercase to ensure case-insensitive comparison.

### `public string? Body`
The raw body content of the request, if applicable. This value is `null` for requests without a body (e.g., `GET` requests). For large payloads, consider truncating or omitting this field to avoid performance overhead.

### `public string? UserId`
The identifier of the authenticated user making the request, if available. This value is `null` for unauthenticated requests.

### `public string ClientIp`
The IP address of the client making the request. This value is derived from the network connection and may be affected by proxies or load balancers.

### `public int StatusCode`
The HTTP status code returned in the response. Example: `200` for success, `404` for not found.

### `public long DurationMs`
The total time taken to process the request, measured in milliseconds. This value includes the time spent in middleware, business logic, and response generation.

### `public ResponseLog Response`
A nested structure containing details about the response sent to the client. See the `ResponseLog` section below for member descriptions.

#### `ResponseLog` Members
- **`public string? Body`**: The raw body content of the response, if applicable. This value is `null` for responses without a body (e.g., `204 No Content`).
- **`public Dictionary<string, string> Headers`**: A collection of key-value pairs representing the HTTP headers included in the response.
- **`public DateTime Timestamp`**: The date and time at which the response was sent.

### `public string? ErrorMessage`
A descriptive message summarizing any error that occurred during request processing. This value is `null` if the request completed successfully.

### `public string OperationId`
A unique identifier for the specific operation or command being executed. This value is used to correlate the request with domain-specific events or workflows.

## Usage

### Example 1: Logging Middleware
