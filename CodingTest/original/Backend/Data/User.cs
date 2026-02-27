namespace Backend.Data;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Password);
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string Email {get; set;} = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}

