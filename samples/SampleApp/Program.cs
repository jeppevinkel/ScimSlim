using SampleApp.Stores;
using ScimSlim.Abstractions;
using ScimSlim.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register the SCIM server with the demo in-memory stores.
// In a real app, swap these for EF Core-backed stores (ScimSlim.EntityFramework).
builder.Services.AddScim<InMemoryScimUserStore, InMemoryScimGroupStore>(opts =>
{
    opts.StaticToken = builder.Configuration["Scim:Token"] ?? "dev-token";
    opts.SupportPatch = true;
});

// AddScim registers the stores as *scoped* (the right lifetime for an EF DbContext).
// These demo stores keep their data in an in-memory dictionary, so they must be
// singletons to survive across requests. A real, persistence-backed store should
// stay scoped — leave these two lines out.
builder.Services.AddSingleton<IScimUserStore, InMemoryScimUserStore>();
builder.Services.AddSingleton<IScimGroupStore, InMemoryScimGroupStore>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "ScimSlim sample app. SCIM endpoints are mounted under /v2.");

// Mount the SCIM endpoints. Point Authentik's SCIM provider at <host>/v2.
app.MapScim("/v2");

app.Run();
