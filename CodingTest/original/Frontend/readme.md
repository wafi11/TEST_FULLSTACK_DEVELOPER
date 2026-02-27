# Frontend — WafCrypto

Blazor WebAssembly (.NET 8) frontend for the WafCrypto authentication system. Runs entirely in the browser via WebAssembly — communicates with the Backend API over HTTP using JWT bearer tokens.

---

## 📁 Folder Structure

```
Frontend/
├── Features/
│   └── AuthService.cs              # Core frontend logic — API calls, JWT storage, lock countdown
│
├── Layout/
│   ├── AuthLayout.razor            # Layout for authentication pages (Login, Register, Lock)
│   ├── AuthLayout.razor.css        # Scoped styles for AuthLayout
│   ├── EmptyLayout.razor           # Bare layout with no nav/footer (Welcome, Home)
│   ├── MainLayout.razor            # Default layout with <main> semantic tag for SEO
│   └── MainLayout.razor.css        # Scoped styles for MainLayout
│
├── Pages/
│   ├── Error.razor                 # Fallback error page
│   ├── Home.razor                  # Landing page — Route: /
│   ├── Home.razor.css              # Scoped styles for Home page
│   ├── Lock.razor                  # Account lock screen — Route: /lock
│   ├── Lock.razor.css              # Scoped styles for Lock page
│   ├── Login.razor                 # Login page — Route: /login
│   ├── Login.razor.css             # Scoped styles for Login page
│   ├── Register.razor              # Registration page — Route: /register
│   ├── Register.razor.css          # Scoped styles for Register page
│   ├── Welcome.razor               # Post-login dashboard — Route: /welcome
│   └── Welcome.razor.css           # Scoped styles for Welcome page
│
├── wwwroot/
│   ├── index.html                  # Entry point HTML — loads Blazor WASM runtime
│   └── css/
│       └── app.css                 # Global styles, CSS variables, reset
│
├── App.razor                       # Root component — bootstraps router and HeadOutlet
├── _Imports.razor                  # Global @using statements for all .razor files
├── Routes.razor                    # Router setup — maps assemblies, sets DefaultLayout
└── Program.cs                      # Entry point — registers HttpClient, AuthService
```

---

## 🧩 Root Files

| File             | Description                                                                                                    |
| ---------------- | -------------------------------------------------------------------------------------------------------------- |
| `App.razor`      | Root Blazor component. Sets up `HeadOutlet` for dynamic `<head>` content (title, meta) and renders the router. |
| `_Imports.razor` | Global imports — any `@using` added here is available in all `.razor` files automatically.                     |
| `Routes.razor`   | Defines `<Router>` component, handles route discovery, sets `DefaultLayout`, and renders `NotFound` fallback.  |
| `Program.cs`     | Registers `HttpClient` pointing to Backend API, registers `AuthService` as scoped dependency.                  |

---

## 🗂️ Layout

| File                | Used By               | Description                                                         |
| ------------------- | --------------------- | ------------------------------------------------------------------- |
| `AuthLayout.razor`  | Login, Register, Lock | Minimal centered layout — no nav or footer, keeps focus on the form |
| `EmptyLayout.razor` | Home, Welcome         | Completely bare layout — no structural chrome                       |
| `MainLayout.razor`  | Default               | Uses semantic `<main>` tag for SEO crawlability                     |

---

## 📄 Pages

| Page             | Route         | Layout      | Description                                                |
| ---------------- | ------------- | ----------- | ---------------------------------------------------------- |
| `Home.razor`     | `/`           | EmptyLayout | Public landing page with hero, ticker, and market table    |
| `Login.razor`    | `/login`      | AuthLayout  | Login form — handles lockout redirect and countdown        |
| `Register.razor` | `/register`   | AuthLayout  | Registration form with backend validation errors           |
| `Welcome.razor`  | `/welcome`    | EmptyLayout | Authenticated dashboard — session countdown, logout        |
| `Lock.razor`     | `/lock`       | AuthLayout  | Account locked screen — reads lockExpiry from localStorage |
| `Error.razor`    | _(automatic)_ | —           | Displayed on unhandled exceptions                          |

---

## ⚙️ Features (`/Features`)

### `AuthService.cs`

Injected as `Scoped` via `Program.cs`. Uses `IJSRuntime` for `localStorage` access and `HttpClient` for API calls.

| Method                         | Description                                                                                                  |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------ |
| `Login(username, password)`    | POST `/api/login` — stores `token`, `loggedInUser`, `lockExpiry`, `sessionExpiry` to localStorage on success |
| `Register(username, password)` | POST `/api/register` — returns validation error from backend                                                 |
| `Logout()`                     | POST `/api/logout` — clears all localStorage keys                                                            |
| `GetCurrentUser()`             | GET `/api/me` with JWT bearer header — returns `UserDto`                                                     |
| `GetLockRemainingSeconds()`    | Reads `lockExpiry` from localStorage — returns seconds remaining                                             |
| `SendWithToken(request)`       | Private helper — injects `Authorization: Bearer <token>` to every request                                    |

### localStorage Keys

| Key             | Set When                | Purpose                                                       |
| --------------- | ----------------------- | ------------------------------------------------------------- |
| `token`         | Login success           | JWT sent in `Authorization` header on every protected request |
| `loggedInUser`  | Login success           | Display current username in UI                                |
| `lockExpiry`    | 3 failed login attempts | Unix timestamp — source of truth for lock countdown           |
| `sessionExpiry` | Login success           | Unix timestamp — source of truth for session countdown        |

---

## 🔄 Auth Flow

```
User fills Login form
    ↓
AuthService.Login() → POST /api/login
    ↓
Backend returns { token, username } or { error, isLocked, remainingSeconds }
    ↓
Success → store token + sessionExpiry to localStorage → /welcome
Locked  → store lockExpiry to localStorage → /lock
Failed  → show error message with attempts remaining
```

---

## 🚀 Getting Started

```bash
# Install dependencies
dotnet restore

# Start the app
dotnet run

# App running at: http://localhost:5053
```

Make sure Backend is running at `http://localhost:5278` before starting Frontend.

---

## 📦 Dependencies

| Package                                                 | Purpose                                               |
| ------------------------------------------------------- | ----------------------------------------------------- |
| `Microsoft.AspNetCore.Components.WebAssembly`           | Blazor WASM runtime                                   |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | Hot reload dev server                                 |
| `Microsoft.JSInterop`                                   | JavaScript interop for localStorage access (built-in) |
