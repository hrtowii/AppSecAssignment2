using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Assignment2.Services;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, RedisService redisService)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionId = context.Session.Id;
        Console.WriteLine($"SessionValidationMiddleware: User ID {userId}, Session ID {sessionId}");

        if (userId != null)
        {
            var storedSessionId = await redisService.GetAsync($"session:{userId}");
            Console.WriteLine($"Stored Session ID for user {userId}: {storedSessionId}");

            if (storedSessionId != sessionId)
            {
                Console.WriteLine("Session ID mismatch. Logging out.");
                await context.SignOutAsync();
                context.Session.Clear();
                context.Response.Redirect("/Registration/Login");
                return;
            }
        }
        await _next(context);
    }
}