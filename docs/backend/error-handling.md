# Backend - Error Handling

---

## Centralized middleware

`ErrorHandlingMiddleware` wraps the entire request pipeline and catches all unhandled exceptions. It converts them to RFC 7807 `ProblemDetails` JSON responses, so every error — regardless of where it originates — has a consistent shape.

**File:** [placey-be/Middleware/ErrorHandlingMiddleware.cs](../../placey-be/Middleware/ErrorHandlingMiddleware.cs)

---

## Exception-to-status mapping

| Exception type                | HTTP status               |
| ----------------------------- | ------------------------- |
| `KeyNotFoundException`        | 404 Not Found             |
| `ResourceDeletedException`    | 410 Gone                  |
| `UnauthorizedAccessException` | 403 Forbidden             |
| `InvalidOperationException`   | 400 Bad Request           |
| Any other exception           | 500 Internal Server Error |

`ResourceDeletedException` is a custom exception class thrown specifically when a soft-deleted resource (listing or user) is accessed directly.

---

## ProblemDetails response shape

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "The check-out date must be after check-in."
}
```

In **Development**, the `detail` field contains the original exception message.  
In **Production**, `detail` is suppressed to avoid leaking internal information.

---

## Model validation (422)

ASP.NET Core's built-in model validation handles `[Required]`, `[EmailAddress]`, `[Range]`, `[MinLength]`, `[StringLength]` annotations on DTO records. Validation failures return **422 Unprocessable Entity** before the controller action is even called.

---

## Service layer conventions

Services communicate errors exclusively through typed exceptions — no `Result<T>` types or error codes. The middleware mapping table above is the single contract between exception types and HTTP status codes.

Controllers never catch exceptions; they let them propagate to the middleware.
