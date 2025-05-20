using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TecChallenge.Application.Extensions;
using TecChallenge.Data.Contexts;
using TecChallenge.Domain.Entities;

namespace TecChallenge.Application.Data;

public static class DbInitializer
{
    public static async Task Initialize(AppDbContext appDbContext, AuthDbContext authDbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Verifica se o banco de dados existe e aplica migrations pendentes
        await authDbContext.Database.MigrateAsync();
        await appDbContext.Database.MigrateAsync();

        // Verifica se já existem dados no banco
        if (await authDbContext.Users.AnyAsync())
        {
            return;
        }

        await SeedRoles(roleManager);
        await SeedUsers(appDbContext, userManager);
        await SeedUserRoles(authDbContext);
    }

    private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new List<ApplicationRole>
        {
            new() { Name = "Admin", NormalizedName = "ADMIN", IsDeleted = false, Level = 0 },
            new() { Name = "User", NormalizedName = "USER", IsDeleted = false, Level = 1 }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedUsers(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        var users = new List<ApplicationUser>
        {
            new()
            {
                UserName = "vinicius_pinheiro05@hotmail.com",
                Email = "vinicius_pinheiro05@hotmail.com",
                EmailConfirmed = true,
                IsDeleted = false,
                FirstAccess = false
            },
            new()
            {
                UserName = "vinicius_pinheiro02@hotmail.com",
                Email = "vinicius_pinheiro02@hotmail.com",
                EmailConfirmed = true,
                IsDeleted = false,
                FirstAccess = false
            }
        };

        foreach (var user in users)
        {
            var existingUser = await userManager.FindByEmailAsync(user.Email);
            if (existingUser != null) continue;
            var result = await userManager.CreateAsync(user, "Default@123");
            if (!result.Succeeded)
            {
                throw new Exception($"Erro ao criar usuário: {string.Join(", ", result.Errors)}");
            }

            context.UserLibraries.Add(new UserLibrary { UserId = user.Id });
            context.Wallets.Add(new UserWallet(user.Id));
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedUserRoles(AuthDbContext context)
    {
        var admin = await context.Users.FirstOrDefaultAsync(u => u.Email == "vinicius_pinheiro05@hotmail.com");
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "vinicius_pinheiro02@hotmail.com");

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");

        if (!await context.UserRoles.AnyAsync())
        {
            var userRoles = new List<IdentityUserRole<Guid>>
            {
                new() { UserId = admin.Id, RoleId = adminRole.Id },
                new() { UserId = user.Id, RoleId = userRole.Id }
            };

            await context.UserRoles.AddRangeAsync(userRoles);
            await context.SaveChangesAsync();
        }
    }
}