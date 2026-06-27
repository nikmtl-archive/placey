using System.Text.Json;

namespace placey_be.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ResourceDeletedException => (StatusCodes.Status410Gone, "Gone"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var problem = new
        {
            type = $"https://httpstatuses.io/{status}",
            title,
            status,
            detail = ex.Message,
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
