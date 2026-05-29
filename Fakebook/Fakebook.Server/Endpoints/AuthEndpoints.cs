using Fakebook.Server.Auth;
using Fakebook.Server.Data;
using Fakebook.Server.Domain;
using Fakebook.Server.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fakebook.Server.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuth(this RouteGroupBuilder api)
    {
        var g = api.MapGroup("/auth").WithTags("Auth");

        g.MapPost("/register", async (RegisterRequest req, FakebookDbContext db, TokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Username, email, password required" });

            if (req.Password.Length < 6)
                return Results.BadRequest(new { error = "Password must be at least 6 chars" });

            var exists = await db.Users.AnyAsync(u => u.Username == req.Username || u.Email == req.Email);
            if (exists)
                return Results.Conflict(new { error = "Username or email already taken" });

            var user = new User
            {
                Username     = req.Username.Trim(),
                Email        = req.Email.Trim().ToLowerInvariant(),
                DisplayName  = string.IsNullOrWhiteSpace(req.DisplayName) ? req.Username : req.DisplayName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(await IssueTokensAsync(user, db, tokens));
        });

        g.MapPost("/login", async (LoginRequest req, FakebookDbContext db, TokenService tokens) =>
        {
            var key = req.UsernameOrEmail?.Trim().ToLowerInvariant() ?? "";
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.UsernameOrEmail || u.Email == key);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            return Results.Ok(await IssueTokensAsync(user, db, tokens));
        });

        g.MapPost("/refresh", async (RefreshRequest req, FakebookDbContext db, TokenService tokens) =>
        {
            var rt = await db.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == req.RefreshToken);

            if (rt is null || rt.RevokedAt != null || rt.ExpiresAt < DateTime.UtcNow || rt.User is null)
                return Results.Unauthorized();

            rt.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(await IssueTokensAsync(rt.User, db, tokens));
        });

        g.MapPost("/logout", async ([FromBody] RefreshRequest req, FakebookDbContext db) =>
        {
            var rt = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == req.RefreshToken);
            if (rt is not null && rt.RevokedAt is null)
            {
                rt.RevokedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            return Results.NoContent();
        });

        return api;
    }

    private static async Task<AuthResponse> IssueTokensAsync(User user, FakebookDbContext db, TokenService tokens)
    {
        var (access, accessExp) = tokens.CreateAccessToken(user);
        var (refresh, refreshExp) = tokens.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = user.Id,
            Token     = refresh,
            ExpiresAt = refreshExp
        });
        await db.SaveChangesAsync();

        return new AuthResponse(
            access, accessExp,
            refresh, refreshExp,
            new UserSummary(user.Id, user.Username, user.DisplayName, user.AvatarUrl));
    }
}
