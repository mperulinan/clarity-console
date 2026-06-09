using Microsoft.AspNetCore.Diagnostics;

namespace Portfolio.API.Middleware;

/// <summary>
/// Maps domain/business rule violations to 400 responses with a readable message.
/// </summary>
public sealed class ArgumentExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var argumentException = FindArgumentException(exception);
        if (argumentException is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new { message = argumentException.Message },
            cancellationToken);

        return true;
    }

    private static ArgumentException? FindArgumentException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is ArgumentException argumentException)
            {
                return argumentException;
            }
        }

        return null;
    }
}
