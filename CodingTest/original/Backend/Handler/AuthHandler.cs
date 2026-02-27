using Backend.Services;
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Backend.Handler;

public static class AuthEndpoints
{
     public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/login", async (
            LoginRequest req,
            AuthService auth) =>
        {
            var (success, error, isLocked, remainingSeconds) = await auth.Login(req.Username, req.Password);
            
            if (isLocked) 
                return Results.BadRequest(new { error, isLocked, remainingSeconds });
            
            if (!success) 
                return Results.BadRequest(new { error, isLocked, remainingSeconds = 0 });

            var token = auth.GenerateToken(req.Username);
            return Results.Ok(new { token, username = req.Username });
        }).DisableAntiforgery();

        app.MapPost("/api/register", async (
            RegisterRequest req,
            AuthService auth) =>
        {
            var (success, error) = await auth.Register(req.Username, req.Password);
            if (!success) return Results.BadRequest(new { error });
            return Results.Ok(new { message = "Register successful" });
        }).DisableAntiforgery();

        app.MapPost("/api/logout", (HttpContext ctx, AuthService auth) =>
        {
            auth.Logout();
            ctx.Response.Cookies.Delete("auth_user"); 
            return Results.Ok();
        }).DisableAntiforgery();

      app.MapGet("/api/me", async (HttpContext ctx, AppDbContext db) =>
{
    var username = ctx.User.Identity?.Name;
    if (string.IsNullOrEmpty(username)) return Results.Unauthorized();

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user == null) return Results.Unauthorized();

    return Results.Ok(new { user.Id, user.Username, user.Email, user.CreatedAt });
}).RequireAuthorization();
    }
}
