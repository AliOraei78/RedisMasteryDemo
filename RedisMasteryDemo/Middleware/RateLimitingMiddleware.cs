public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingService _rateLimitService;

    public RateLimitingMiddleware(
        RequestDelegate next,
        RateLimitingService rateLimitService)
    {
        _next = next;
        _rateLimitService = rateLimitService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Using the user's IP address or UserId
        string clientKey =
            context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        if (!await _rateLimitService.IsAllowedAsync(clientKey))
        {
            context.Response.StatusCode =
                StatusCodes.Status429TooManyRequests;

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too Many Requests",
                message = "Please wait a moment before trying again.",
                remaining = 0
            });

            return;
        }

        await _next(context);
    }
}