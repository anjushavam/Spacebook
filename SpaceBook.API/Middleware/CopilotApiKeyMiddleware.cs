using Microsoft.AspNetCore.Http;

namespace SpaceBook.API.Middleware;

public class CopilotApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    private const string HeaderName = "X-Copilot-Key";

    public CopilotApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IConfiguration configuration)
    {
        // Only protect Copilot APIs
        if (context.Request.Path.StartsWithSegments("/api/copilot"))
        {
            var configuredKey =
                configuration["COPILOT_API_KEY"];

            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                context.Response.StatusCode =
                    StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Copilot API key is not configured."
                });

                return;
            }

            if (!context.Request.Headers.TryGetValue(
                    HeaderName,
                    out var providedKey))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Copilot API key is required."
                });

                return;
            }

            if (!string.Equals(
                    providedKey.ToString(),
                    configuredKey,
                    StringComparison.Ordinal))
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Invalid Copilot API key."
                });

                return;
            }
        }

        await _next(context);
    }
}