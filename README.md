# ScimSlim

[![NuGet Version](https://img.shields.io/nuget/v/ScimSlim?style=flat&logo=nuget&label=ScimSlim)](https://www.nuget.org/packages/ScimSlim)
[![NuGet Version](https://img.shields.io/nuget/v/ScimSlim.EntityFramework?style=flat&logo=nuget&label=ScimSlim.EntityFramework)](https://www.nuget.org/packages/ScimSlim.EntityFramework)

Lightweight **SCIM 2.0 server** middleware for ASP.NET Core 10+, built for use with
[Authentik](https://goauthentik.io/) in homelab environments. MIT licensed.

ScimSlim is **not** a full RFC 7643/7644 implementation. It is scoped specifically to
what Authentik's SCIM provider sends and expects, so you can stand up a provisioning
endpoint in a few lines instead of pulling in a heavyweight identity stack.

```
Authentik ──SCIM──► Your App (SCIM Server endpoint)
Authentik ◄──OIDC── Your App (OIDC Client)
```

Authentik acts as the SCIM client (pushing users/groups to your app) and OIDC provider.
Your app acts as the SCIM server (receiving provisioning) and OIDC client (delegating auth).

## Packages

| Package | Description |
| --- | --- |
| `ScimSlim` | The SCIM server middleware, models, endpoints and abstractions. |
| `ScimSlim.EntityFramework` | Drop-in EF Core-backed `IScimUserStore` / `IScimGroupStore`. |

## Install

```bash
dotnet add package ScimSlim
# optional EF Core store implementation
dotnet add package ScimSlim.EntityFramework
```

## Quick start

Implement the two store interfaces against your data layer, then register and map SCIM:

```csharp
using ScimSlim.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScim<MyAppUserStore, MyAppGroupStore>(opts =>
{
    opts.StaticToken = builder.Configuration["Scim:Token"]!;
    opts.SupportPatch = true;
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapScim("/v2"); // point Authentik's SCIM provider at https://your-app.com/v2

app.Run();
```

`AddScim` registers a `ScimToken` authentication scheme (validating the
`Authorization: Bearer <token>` header against `ScimOptions.StaticToken`) and a matching
authorization policy applied to every SCIM endpoint.

### Implementing a store

Stores are the only thing you must write. Implement `IScimUserStore` and `IScimGroupStore`
against EF Core, Dapper, or whatever you use:

```csharp
public class MyAppUserStore : IScimUserStore
{
    public Task<ScimUser?> GetByIdAsync(string id) { /* ... */ }
    public Task<ScimUser?> GetByExternalIdAsync(string externalId) { /* ... */ }
    // ... the rest of the interface
}
```

Two helpers cut down on boilerplate:

- `ScimSlim.Filtering.ScimFilter.TryParse(filter, out var f)` parses the simple
  `attribute eq "value"` form that Authentik sends.
- `ScimSlim.Patching.ScimPatch.Apply(user, patch)` / `Apply(group, patch)` applies SCIM
  PatchOp operations to an in-memory `ScimUser` / `ScimGroup` (including group member
  add/remove).

See [`samples/SampleApp`](samples/SampleApp) for complete in-memory store implementations.

## Using the EF Core stores

`ScimSlim.EntityFramework` ships ready-made stores. Add the SCIM entities to your context
and register the stores:

```csharp
using Microsoft.EntityFrameworkCore;
using ScimSlim.EntityFramework;
using ScimSlim.EntityFramework.Entities;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IScimDbContext
{
    public DbSet<ScimUserEntity> ScimUsers => Set<ScimUserEntity>();
    public DbSet<ScimGroupEntity> ScimGroups => Set<ScimGroupEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyScimModel();
}
```

```csharp
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=scim.db"));

builder.Services.AddScim<EfScimUserStore, EfScimGroupStore>(opts =>
{
    opts.StaticToken = builder.Configuration["Scim:Token"]!;
});
builder.Services.AddScimEntityFrameworkStores<AppDbContext>();
```

### Soft-deleting users

`AddScimEntityFrameworkStores` takes an optional `EfScimStoreOptions` configure delegate:

```csharp
builder.Services.AddScimEntityFrameworkStores<AppDbContext>(opts =>
{
    opts.SoftDeleteUsers = true; // default
});
```

- `SoftDeleteUsers = true` (the default): a SCIM `DELETE` flags the `ScimUserEntity` row
  (`IsDeleted`, `DeletedAt`) instead of removing it. Deleted users are excluded from every
  read (`GET`, `PUT`, `PATCH`, list) as if they didn't exist, so this is transparent to
  Authentik — it just keeps the row around for your own auditing/history.
- Since Authentik keeps the same `id` for a user across its lifetime, re-adding a
  previously-deleted user to a provisioned group causes Authentik to `POST` a `Create`
  with that same `id`. The EF store detects this and resurrects the existing row (clearing
  `IsDeleted`/`DeletedAt`) instead of failing on the id/username collision. Resurrection only
  matches on the SCIM-assigned `id` — never on `userName`, since that's user-chosen and
  reusable, and matching on it would let a new registration take over a deleted account's
  history.
- `SoftDeleteUsers = false` restores plain hard deletion (the previous default) for new
  `DELETE` calls. Flip it back to `true` at any time without touching existing rows — deleted
  users stay hidden from reads regardless of the current setting, so toggling the option never
  resurrects anyone on its own. There's no purge path exposed through the SCIM API itself — the
  `DELETE` endpoint 404s for a user that's already gone, soft-deleted or not — so cleaning up
  old soft-deleted rows is a decision you make yourself. `AddScimEntityFrameworkStores` already
  registers `IScimDbContext`, so your own maintenance code can inject it and remove
  `ScimUserEntity` rows directly.

## SCIM endpoints

`MapScim` mounts the minimum set Authentik needs under the given prefix (default `/v2`):

| Method | Path |
| --- | --- |
| GET | `/v2/Users` (supports `?filter=userName eq "x"`, `?startIndex`, `?count`) |
| GET | `/v2/Users/{id}` |
| POST | `/v2/Users` |
| PUT | `/v2/Users/{id}` |
| PATCH | `/v2/Users/{id}` |
| DELETE | `/v2/Users/{id}` |
| GET | `/v2/Groups` |
| GET | `/v2/Groups/{id}` |
| POST | `/v2/Groups` |
| PUT | `/v2/Groups/{id}` |
| PATCH | `/v2/Groups/{id}` |
| DELETE | `/v2/Groups/{id}` |
| GET | `/v2/ServiceProviderConfig` |

All responses use the `application/scim+json` media type, and errors follow the SCIM
error envelope (`urn:ietf:params:scim:api:messages:2.0:Error`).

## Linking OIDC login to a provisioned user

Authentik's OIDC `sub` claim matches the SCIM `externalId`, so you can resolve the local
record on login:

```csharp
var externalId = User.FindFirst("sub")?.Value;
var localUser = await userStore.GetByExternalIdAsync(externalId);
```

## Authentik notes

- Authentik is the SCIM **client**; your app is the **server**.
- Authentik performs a full sync periodically — **all operations must be idempotent**.
- `externalId` is a stable, hashed user ID from Authentik. Use it to link the Authentik
  identity to your local record, and it matches the OIDC `sub` claim.
- Authentik uses `PATCH` for group member add/remove operations.
- Static Bearer token authentication is the simplest Authentik SCIM auth option.
- Point Authentik's SCIM provider at `https://your-app.com/v2`.

## Versioning

`0.x` is initial development — breaking changes happen freely until the API stabilizes, at which point it graduates to `1.0.0`.

## License

[MIT](LICENSE)
