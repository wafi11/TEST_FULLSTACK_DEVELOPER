using Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using BCrypt.Net;

namespace Backend.Services;

public class AuthService
{
    private readonly AppDbContext _db;
private readonly IConfiguration _config;

    public static string? LoggedInUser { get; set; }
    private static Dictionary<string, int> _failedAttempts = new();
    private static Dictionary<string, DateTime> _lockedUntil = new();

public AuthService(AppDbContext db, IConfiguration config)

    {
        _db = db;
           _config = config;

    }

      public string GenerateToken(string username) 
{
    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

    var token = new JwtSecurityToken(
        issuer: _config["Jwt:Issuer"], 
        claims: new[]
        {
            new Claim(ClaimTypes.Name, username),
        },
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}


    public async Task<(bool Success, string Error)> Register(string username, string password)
    {
        // validation input
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username is required.");

        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required.");

        if (username.Trim().Length < 3 || username.Trim().Length > 30)
            return (false, "Username must be between 3 and 30 characters.");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters.");

        if (!Regex.IsMatch(username.Trim(), @"^[a-zA-Z0-9_.-]+$"))
            return (false, "Username can only contain letters, numbers, underscores, hyphens, and dots.");

        if (await _db.Users.AnyAsync(u => u.Username == username.Trim()))
            return (false, "Username already exists.");

    // start transaction
    using var transaction = await _db.Database.BeginTransactionAsync();
    try
    {
        // define user to prepare input to database
        var user = new User
        {
            Username = username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Email = username.Trim()
        };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(); 

        // define session to prepare input to database
        var session = new Session
        {
            UserId = user.Id,
        };
            _db.Sessions.Add(session);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync(); 
        return (true, "");
    }
    catch
    {
        // if any problems rollback to cancel transaction
        await transaction.RollbackAsync(); 
        return (false, "Registration failed. Please try again.");
    }
}
    public async Task<(bool Success, string Error, bool IsLocked, int RemainingSeconds)> Login(string username, string password)
{
    if (_lockedUntil.TryGetValue(username, out var lockedUntil))
    {
        if (DateTime.UtcNow < lockedUntil)
        {
            var remaining = (int)(lockedUntil - DateTime.UtcNow).TotalSeconds;
            return (false, "Account locked.", true, remaining); // ← kirim remaining
        }
        else
        {
            _lockedUntil.Remove(username);
            _failedAttempts.Remove(username);
        }
    }

    var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username.Trim());
    if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
    {
        _failedAttempts.TryGetValue(username, out var attempts);
        attempts++;
        _failedAttempts[username] = attempts;

        if (attempts >= 3)
        {
            _lockedUntil[username] = DateTime.UtcNow.AddSeconds(30);
            _failedAttempts.Remove(username);
            return (false, "Too many attempts. Account locked for 30 seconds.", true, 30);
        }

        int remaining = 3 - attempts;
        return (false, $"Invalid credentials. {remaining} attempt(s) remaining.", false, 0);
    }

    _failedAttempts.Remove(username);
    _lockedUntil.Remove(username);
    LoggedInUser = username;
    return (true, "", false, 0);
}

    // define logout users
    public void Logout() => LoggedInUser = null;

    // define function checking user
    public async Task<User?> GetCurrentUser(HttpContext ctx)
    {
        ctx.Request.Cookies.TryGetValue("auth_user", out var username);
        if (string.IsNullOrEmpty(username)) return null;

        return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    // checking key username is locked or not
    public bool IsLocked(string username)
    {
        if (_lockedUntil.TryGetValue(username, out var lockedUntil))
            return DateTime.UtcNow < lockedUntil;
        return false;
    }
}