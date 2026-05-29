using System.Text;
using DotNetEnv;
using Fakebook.Server.Auth;
using Fakebook.Server.Data;
using Fakebook.Server.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// Pull .env into process env so JWT_* / ConnectionStrings__fakebookdb resolve when running
// the server directly (outside the AppHost). AppHost already loads .env itself.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClientBuilder("cache").WithOutputCache();
builder.AddNpgsqlDbContext<FakebookDbContext>("fakebookdb");

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .SetIsOriginAllowed(_ => true)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var jwt = new JwtOptions
{
    Issuer              = builder.Configuration["JWT_ISSUER"]              ?? "fakebook",
    Audience            = builder.Configuration["JWT_AUDIENCE"]            ?? "fakebook-clients",
    Secret              = builder.Configuration["JWT_SECRET"]              ?? "dev-only-secret-change-me-32-chars-minimum-xxx",
    AccessTokenMinutes  = int.TryParse(builder.Configuration["JWT_ACCESS_TOKEN_MINUTES"], out var am) ? am : 60,
    RefreshTokenDays    = int.TryParse(builder.Configuration["JWT_REFRESH_TOKEN_DAYS"],   out var rd) ? rd : 14
};
builder.Services.Configure<JwtOptions>(opt =>
{
    opt.Issuer = jwt.Issuer; opt.Audience = jwt.Audience; opt.Secret = jwt.Secret;
    opt.AccessTokenMinutes = jwt.AccessTokenMinutes; opt.RefreshTokenDays = jwt.RefreshTokenDays;
});
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwt.Issuer,
            ValidAudience            = jwt.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew                = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

// Apply migrations + seed on boot so a fresh checkout boots without manual `dotnet ef` steps.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FakebookDbContext>();
    await db.Database.MigrateAsync();
    await Seeder.SeedAsync(db);
}

var api = app.MapGroup("/api");
api.MapAuth();
api.MapUsers();
api.MapFriends();
api.MapPosts();
api.MapFeed();

api.MapGet("/", () => Results.Ok(new { name = "Fakebook API", version = "0.1.0" }));

app.MapDefaultEndpoints();
app.UseFileServer();
app.MapFallbackToFile("index.html");

app.Run();
