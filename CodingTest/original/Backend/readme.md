# Backend — WafCrypto API

ASP.NET Core Web API (.NET 8) serving as the backend for WafCrypto authentication system. Handles user registration, login, JWT token generation, and account lockout protection.

---

## 📁 Folder Structure

```
Backend/
├── Data/
│   ├── AppDbContext.cs         # EF Core DB context — manages SQLite connection and DbSets
│   ├── Session.cs              # Session entity (Id, UserId, CreatedAt)
│   └── User.cs                 # User entity (Id, Username, Email, PasswordHash, CreatedAt)
│
├── Handler/
│   └── AuthHandler.cs          # Minimal API route definitions — maps all /api/* endpoints
│
├── Services/
│   └── AuthService.cs          # Core business logic — Register, Login, Logout, GenerateToken
│
├── Migrations/                 # EF Core auto-generated migration files
├── Properties/                 # Launch settings (ports, environment)
├── Backend.http                # HTTP request file for API testing in VS Code / Rider
└── Program.cs                  # Entry point — middleware pipeline, DI registration, JWT config
```

---

## 🗃️ Data Layer (`/Data`)

### `AppDbContext.cs`

Extends `DbContext`. Defines two `DbSet` properties mapped to SQLite tables.

```csharp
public DbSet<User> Users => Set<User>();
public DbSet<Session> Sessions => Set<Session>();
```

### `User.cs`

| Property       | Type                   | Description                       |
| -------------- | ---------------------- | --------------------------------- |
| `Id`           | `int`                  | Primary key, auto-increment       |
| `Username`     | `string`               | Unique, 3–30 chars, alphanumeric  |
| `Email`        | `string`               | VARCHAR(255)                      |
| `PasswordHash` | `string`               | BCrypt hashed password            |
| `CreatedAt`    | `DateTime`             | UTC registration timestamp        |
| `Sessions`     | `ICollection<Session>` | Navigation property — one-to-many |

### `Session.cs`

| Property    | Type       | Description                               |
| ----------- | ---------- | ----------------------------------------- |
| `Id`        | `int`      | Primary key, auto-increment               |
| `UserId`    | `int`      | Foreign key → `Users.Id` (CASCADE DELETE) |
| `User`      | `User`     | Navigation property — belongs to User     |
| `CreatedAt` | `DateTime` | UTC session creation timestamp            |

---

## 🔀 Handler (`/Handler`)

### `AuthHandler.cs`

Static class with `MapAuthEndpoints()` extension method. Registered in `Program.cs` via `app.MapAuthEndpoints()`.

| Method | Route           | Auth   | Description                               |
| ------ | --------------- | ------ | ----------------------------------------- |
| `POST` | `/api/register` | ❌     | Create new user with validation           |
| `POST` | `/api/login`    | ❌     | Authenticate user, return JWT token       |
| `POST` | `/api/logout`   | ❌     | Clear session state                       |
| `GET`  | `/api/me`       | ✅ JWT | Return current authenticated user details |

---

## ⚙️ Services (`/Services`)

### `AuthService.cs`

Injected as `Scoped` — new instance per HTTP request.

| Method                         | Description                                                                                                     |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------- |
| `Register(username, password)` | Validates input, checks duplicate, hashes password with BCrypt, saves User + Session in a single DB transaction |
| `Login(username, password)`    | Queries DB, verifies BCrypt hash, tracks failed attempts, returns `remainingSeconds` on lockout                 |
| `Logout()`                     | Resets `LoggedInUser` static state                                                                              |
| `GenerateToken(username)`      | Creates HMAC-SHA256 signed JWT with 7-day expiry                                                                |
| `GetCurrentUser(username)`     | Fetches User entity from DB by username                                                                         |
| `IsLocked(username)`           | Checks if username is currently locked out                                                                      |

**Account Lockout Logic:**

- 3 failed attempts → locked for 30 seconds
- Lock state stored in static `Dictionary<string, DateTime>`
- `remainingSeconds` returned in API response for frontend countdown

---

## 🚀 `Program.cs`

Middleware pipeline order (order matters):

```
AddAuthentication (JWT Bearer)
AddAuthorization
AddDbContext (SQLite)
AddScoped<AuthService>
AddCors
─────────────────────────────
app.UseCors()
app.UseHttpsRedirection()
app.UseAuthentication()   ← must be before Authorization
app.UseAuthorization()    ← must be before MapAuthEndpoints
app.MapAuthEndpoints()
```

---

## 🧪 `Backend.http`

Used for quick API testing directly in VS Code (REST Client extension) or JetBrains Rider.

```http
### Register
POST http://localhost:5278/api/register
Content-Type: application/json

{
  "username": "wafiuddin",
  "password": "P@ssw0rd1"
}

### Login
POST http://localhost:5278/api/login
Content-Type: application/json

{
  "username": "wafiuddin",
  "password": "P@ssw0rd1"
}

### Get Current User
GET http://localhost:5278/api/me
Authorization: Bearer {{token}}
```

---

## 🔐 Security Notes

- Passwords are **never stored in plain text** — BCrypt hashing applied before any DB write
- EF Core uses **parameterized queries** — SQL injection protected by default
- JWT tokens are **HMAC-SHA256 signed** — tamper-proof, validated on every protected request
- CORS restricted to **frontend origin only** — no wildcard origins

---

## ⚡ Getting Started

```bash
# Install dependencies
dotnet restore

# Run database migrations
dotnet ef database update

# Start the API
dotnet run

# API:     http://localhost:5278
# Swagger: http://localhost:5278/swagger
```

---

## 📦 Dependencies

| Package                                         | Version | Purpose                    |
| ----------------------------------------------- | ------- | -------------------------- |
| `Microsoft.EntityFrameworkCore.Sqlite`          | 8.0.0   | SQLite database provider   |
| `Microsoft.EntityFrameworkCore.Design`          | 8.0.0   | EF Core migrations tooling |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0   | JWT middleware             |
| `System.IdentityModel.Tokens.Jwt`               | —       | JWT token generation       |
| `BCrypt.Net-Next`                               | 4.1.0   | Password hashing           |
