# JWTDemo: Token-Based Authentication in ASP.NET Core MVC

**CSCI 3110**: companion application for the *Token-Based Authentication with JWT* notes
(`CSCI310-JWTAuthentication.docx`).

This app shows the complete JWT flow from the notes:

1. The client sends credentials (email and password) to the server.
2. The server checks them with ASP.NET Core Identity and returns a **signed JWT**.
3. The server **does not store** the token. The app is stateless.
4. The client stores the token (here, in `localStorage`).
5. The client sends `Authorization: Bearer <token>` on later requests.
6. The server checks the signature, issuer, audience and expiration, then authorizes the request.

The home page is an interactive client. You can log in as different users, see the
token decoded, call protected endpoints with or without the token, and try to forge an
Administrator token.

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/) with the **C# Dev Kit** extension
  (`ms-dotnettools.csdevkit`). Installing it also installs the **C#** extension.
- You do not need to install a database. The app uses SQLite and creates `Data/jwtapp.db` when it starts.

## Running the app in VS Code

1. In VS Code, choose **File → Open Folder...** and open the `JWTDemo` folder (the one that contains `JWTDemo.csproj`).
2. Open the integrated terminal with **Terminal → New Terminal** (or <kbd>Ctrl</kbd>+<kbd>`</kbd>).
3. The first time only, trust the HTTPS development certificate:
   ```bash
   dotnet dev-certs https --trust
   ```
4. Start the app:
   ```bash
   dotnet run --launch-profile https
   ```
5. <kbd>Ctrl</kbd>+click the URL in the terminal, or browse to **https://localhost:5041**.
   To use plain HTTP instead, run `dotnet run --launch-profile http` and browse to **http://localhost:5040**.
6. To stop the app, click in the terminal and press <kbd>Ctrl</kbd>+<kbd>C</kbd>.

> **Tip:** Use `dotnet watch --launch-profile https` instead of `dotnet run`. It restarts the app
> automatically each time you save a file.

### Debugging with breakpoints

1. Set a breakpoint by clicking in the left margin, for example on the `CreateToken` line in
   `Services/JwtTokenService.cs` or the first line of `Login` in `Controllers/Api/AuthController.cs`.
2. Press <kbd>F5</kbd>. If VS Code asks for a debugger, choose **C#**, then choose the **https** launch configuration for **JWTDemo**.
3. Log in on the demo page. Execution stops at the breakpoint, and you can inspect `claims`, `roles`
   and the finished `token` in the **Variables** panel.

You can also watch the requests in the browser's DevTools (<kbd>F12</kbd> → **Network** tab).

### Demo accounts

These accounts are created automatically on first run by `Services/SeedData.cs`:

| Email                 | Password       | Role          |
|-----------------------|----------------|---------------|
| `admin@example.com`   | `Password123!` | Administrator |
| `manager@example.com` | `Password123!` | Manager       |
| `user@example.com`    | `Password123!` | User          |

To reset the database, stop the app and delete the `Data/jwtapp.db*` files.

---

## Project structure

```
JWTDemo/
├── Program.cs                        ← Identity + JWT Bearer setup, middleware pipeline
├── appsettings.json                  ← "Jwt" section (Key, Issuer, Audience, ExpiresInMinutes)
├── Controllers/
│   ├── HomeController.cs             ← serves the demo page (MVC view)
│   └── Api/
│       ├── AuthController.cs         ← POST /api/auth/login, GET /api/auth/me
│       └── ProductsController.cs     ← protected endpoints ([Authorize], roles, [AllowAnonymous])
├── Data/
│   └── jwtapp.db                     ← SQLite database (created on first run)
├── Models/
│   ├── DTO/LoginRequest.cs           ← { email, password } request body
│   └── Entities/ApplicationUser.cs   ← IdentityUser + DisplayName
├── Services/
│   ├── ApplicationDbContext.cs       ← IdentityDbContext<ApplicationUser> (SQLite)
│   ├── SeedData.cs                   ← creates roles and demo users
│   ├── ITokenService.cs              ← "create a token for this user"
│   └── JwtTokenService.cs            ← builds and signs the JWT
├── Views/Home/Index.cshtml           ← demo UI
└── wwwroot/js/jwt-demo.js            ← client: login, store token, send Bearer header, logout
```

### NuGet packages

| Package | Purpose |
|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | Validates incoming `Bearer` tokens |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Users, password hashing, roles |
| `Microsoft.EntityFrameworkCore.Sqlite` | Stores the Identity tables in a SQLite file |

---

## API endpoints

| Method & route | Who can call it | Attribute |
|---|---|---|
| `POST /api/auth/login` | Anyone | (none). Returns `{ "token": "..." }` or **401** |
| `GET /api/auth/me` | Any valid token | `[Authorize]`. Returns the claims the server read from the token |
| `GET /api/products/featured` | Anyone | `[AllowAnonymous]` overrides the controller's `[Authorize]` |
| `GET /api/products` | Any valid token | `[Authorize]` on the controller |
| `PUT /api/products/{id}` | Administrator **or** Manager | `[Authorize(Roles = "Administrator,Manager")]` |
| `DELETE /api/products/{id}` | Administrator only | `[Authorize(Roles = "Administrator")]` |

### 401 and 403

- **401 Unauthorized** means *"I don't know who you are."* The token is missing, expired, or has a bad signature.
- **403 Forbidden** means *"I know who you are, but you aren't allowed to do this."* The token is valid but doesn't have the required role.

---

## Classroom walkthrough

Use the home page and keep the browser DevTools **Network** tab open.

1. **Anonymous access.** Turn off "Send the Authorization header". `GET /api/products/featured`
   returns **200** and `GET /api/products` returns **401**.
2. **Log in** as `user@example.com`. The token appears in three colors
   (header.payload.signature), and the decoded header and payload appear below it.
   The browser decoded them without the secret key. **Base64URL is not encryption.**
3. **Send the token.** Turn the header back on. `GET /api/products` and `GET /api/auth/me` return **200**.
   `/me` shows that the server got the email and role from the token. It did not query the database.
4. **Roles.** As `user`, `PUT` and `DELETE` return **403**. Log in as `manager`: `PUT` returns **200** and
   `DELETE` returns **403**. Log in as `admin`: both return **200**.
5. **Tampering.** Log in as `user` and click **Promote myself to Administrator & DELETE**.
   The page changes the payload to `"role":"Administrator"` but reuses the original signature.
   The server returns **401** with `WWW-Authenticate: Bearer error="invalid_token"`, because the
   signature no longer matches the content.
6. **Logout.** Click **Log out**. The token is deleted from `localStorage` and protected calls return **401**.
   Discuss this: if you had copied the token first, it would *still work* until `exp`, because
   the server has no session to destroy (see "Logging Out" in the notes).
7. **Expiration.** Set `"ExpiresInMinutes": 1` in `appsettings.json`, restart, log in, and wait
   for the badge to say *Expired*. Protected calls then return **401** even though the token is unchanged.

### Try it with curl

Run these commands in a second terminal in VS Code, while the app is running in the first one.
Click **+** in the terminal panel to open it, and choose **Git Bash** from the drop-down.
The quoting in these examples doesn't work in PowerShell.

```bash
# 1. Log in
curl -k -X POST https://localhost:5041/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"admin@example.com","password":"Password123!"}'

# 2. Call a protected endpoint (paste the token)
curl -k https://localhost:5041/api/products -H "Authorization: Bearer <token>"
```

You can also paste a token into [jwt.io](https://jwt.io) and enter the `Jwt:Key` from
`appsettings.json` as the secret to verify the signature.

---

## Differences from the code in the notes

The app follows the notes closely. A few small changes were needed to make it work
correctly. Each one is a good discussion point.

1. **`AddIdentityCore` instead of `AddIdentity`** (`Program.cs`). `AddIdentity` also registers
   Identity's **cookie** authentication and makes it the default scheme. `AddIdentityCore` registers
   only `UserManager`, `RoleManager` and `SignInManager`, so JWT is the only authentication in the app.
   `CheckPasswordSignInAsync` only checks the password. It never creates a cookie.

2. **`MapInboundClaims = false`, `NameClaimType = "sub"`, `RoleClaimType = "role"`** (`Program.cs`).
   By default, the JWT handler renames incoming claims to long Microsoft URIs. For example, `sub`
   becomes `http://schemas.xmlsoap.org/.../nameidentifier`. With the default setting,
   `User.Identity.Name` would be **null**. These settings keep the claim names exactly as they
   appear in the token and tell ASP.NET Core which claims hold the user's name and roles.

3. **Role claims are written as `"role"`, not `ClaimTypes.Role`** (`JwtTokenService.cs`).
   If you build the token with `new JwtSecurityToken(...)` and use `ClaimTypes.Role`, the payload
   contains the full URI `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` instead of
   `role`. Every `[Authorize(Roles = ...)]` check would then fail with 403. Using `"role"` also
   gives the payload shown in the notes: `{"sub":"...","role":"User",...}`.

4. **`ClockSkew = TimeSpan.Zero`** (`Program.cs`). By default, ASP.NET Core accepts tokens for
   **5 minutes after** they expire, to allow for differences between server clocks. Setting it
   to zero makes the expiration demo in the walkthrough work exactly on time.

5. **Extra claims** (`JwtTokenService.cs`). The token also includes `iat` (issued-at) and `name` (display name),
   so students can see more of the registered claims from the notes.

6. **`EnsureCreated` instead of migrations** (`SeedData.cs`). This keeps setup to one `dotnet run`.
   A real app would use `dotnet ef migrations add` and `dotnet ef database update`.

---

## Security notes (this is a demo)

- **The signing key is in `appsettings.json`** so the app runs with no setup. In a real app,
  never commit it. Use [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
  during development:
  (run these in the VS Code terminal)
  ```bash
  dotnet user-secrets init
  dotnet user-secrets set "Jwt:Key" "<a long random value>"
  ```
  In production, use environment variables (`Jwt__Key`) or a vault such as Azure Key Vault.
- HS256 requires a key of **at least 256 bits (32 bytes)**. If the key is shorter, token creation throws an exception.
- **`localStorage` can be read by any JavaScript on the page**, so an XSS bug could steal the token.
  See the "Cookie vs. Token" table in the notes.
- Logout here is **client-side deletion only**. Real apps combine short-lived access tokens with
  refresh tokens or a `jti` blacklist.
- The demo password is intentionally simple and is shown on screen. Never do this in a real app.
