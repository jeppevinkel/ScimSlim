using Microsoft.EntityFrameworkCore;
using ScimSlim.EntityFramework.Entities;

namespace ScimSlim.EntityFramework;

/// <summary>
/// <c>ModelBuilder</c> configuration for the SCIM EF entities. Call from your
/// <c>DbContext.OnModelCreating</c>.
/// </summary>
public static class ScimModelBuilderExtensions
{
    public static ModelBuilder ApplyScimModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScimUserEntity>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.UserName).IsUnique();
            b.HasIndex(u => u.ExternalId);
            b.HasMany(u => u.Emails)
                .WithOne()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScimEmailEntity>(b => b.HasKey(e => e.Id));

        modelBuilder.Entity<ScimGroupEntity>(b =>
        {
            b.HasKey(g => g.Id);
            b.HasIndex(g => g.ExternalId);
            b.HasMany(g => g.Members)
                .WithOne()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ScimGroupMemberEntity>(b =>
        {
            b.HasKey(m => m.Id);
            b.HasIndex(m => new { m.Value, m.GroupId }).IsUnique();
        });

        return modelBuilder;
    }
}
