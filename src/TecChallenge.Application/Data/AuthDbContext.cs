using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TecChallenge.Application.Extensions;

namespace TecChallenge.Application.Data;

public sealed class AuthDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        foreach (
            var relationship in builder
                .Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys())
        )
            relationship.DeleteBehavior = DeleteBehavior.ClientSetNull;

        base.OnModelCreating(builder);

        builder.Entity<ApplicationRole>(entity => { entity.ToTable("Role"); });

        builder.Entity<ApplicationUser>(entity => { entity.ToTable("User"); });

        builder.Entity<IdentityUserRole<Guid>>(entity => { entity.ToTable("UserRole"); });

        builder.Entity<IdentityUserClaim<Guid>>(entity => { entity.ToTable("UserClaim"); });

        builder.Entity<IdentityUserLogin<Guid>>(entity => { entity.ToTable("UserLogin"); });

        builder.Entity<IdentityRoleClaim<Guid>>(entity => { entity.ToTable("RoleClaim"); });

        builder.Entity<IdentityUserToken<Guid>>(entity => { entity.ToTable("UserToken"); });
    }
}