using DotNetEnv;

// Load .env so the AppHost can forward DB / JWT vars to child resources.
Env.TraversePath().Load();

var builder = DistributedApplication.CreateBuilder(args);

var pgUser     = builder.AddParameter("pg-user",     Environment.GetEnvironmentVariable("POSTGRES_USER")     ?? "app_backend", secret: true);
var pgPassword = builder.AddParameter("pg-password", Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "admin",       secret: true);

// Connect to the existing local Postgres instance rather than spinning up a container,
// because Postgres is already running on the dev box (port 5432).
var postgres = builder.AddConnectionString(
    "fakebookdb",
    ReferenceExpression.Create(
        $"Host=localhost;Port=5432;Database=fakebook_db;Username={pgUser};Password={pgPassword};Include Error Detail=true"));

var cache = builder.AddRedis("cache");

var server = builder.AddProject<Projects.Fakebook_Server>("server")
    .WithReference(cache)
    .WithReference(postgres)
    .WaitFor(cache)
    .WithEnvironment("JWT_ISSUER",                 Environment.GetEnvironmentVariable("JWT_ISSUER")                 ?? "fakebook")
    .WithEnvironment("JWT_AUDIENCE",               Environment.GetEnvironmentVariable("JWT_AUDIENCE")               ?? "fakebook-clients")
    .WithEnvironment("JWT_SECRET",                 Environment.GetEnvironmentVariable("JWT_SECRET")                 ?? "dev-only-secret-change-me-32-chars-minimum-xxx")
    .WithEnvironment("JWT_ACCESS_TOKEN_MINUTES",   Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_MINUTES")   ?? "60")
    .WithEnvironment("JWT_REFRESH_TOKEN_DAYS",     Environment.GetEnvironmentVariable("JWT_REFRESH_TOKEN_DAYS")     ?? "14")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
