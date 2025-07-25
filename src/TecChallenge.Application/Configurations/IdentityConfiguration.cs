using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using TecChallenge.Application.Data;
using TecChallenge.Application.Extensions;
using TecChallenge.Data.Contexts;

namespace TecChallenge.Application.Configurations;

public static class IdentityConfig
{
    public static void AddIdentityConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddDbContext<AuthDbContext>(options =>
                options
                    .UseNpgsql(
                        configuration.GetConnectionString("DefaultConnection"),
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    )
                    .ConfigureWarnings(w =>
                        w.Throw(RelationalEventId.MultipleCollectionIncludeWarning)
                    )
                    .UseLazyLoadingProxies()
            )
            .AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddSignInManager<ApplicationSignInManager>()
            .AddDefaultTokenProviders();

        var jwtAppSettingOptions = configuration.GetSection(nameof(JwtOptions));

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                configuration.GetValue<string>("ApplicationKey")
                    ?? throw new InvalidOperationException()
            )
        );

        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwtAppSettingOptions[nameof(JwtOptions.Issuer)];
            options.Audience = jwtAppSettingOptions[nameof(JwtOptions.Audience)];
            options.SecurityKey = securityKey;
            options.SigningCredentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );
            options.AccessTokenExpiration = int.Parse(
                jwtAppSettingOptions[nameof(JwtOptions.AccessTokenExpiration)]
            );
            options.RefreshTokenExpiration = int.Parse(
                jwtAppSettingOptions[nameof(JwtOptions.RefreshTokenExpiration)]
            );
        });

        services
            .AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = true;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwtAppSettingOptions[nameof(JwtOptions.Issuer)],
                    ValidateAudience = false,
                    ValidAudience = jwtAppSettingOptions[nameof(JwtOptions.Audience)],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });
    }

    public static async Task InitializeIdentityDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var appDbContext = services.GetRequiredService<AppDbContext>();
            var authDbContext = services.GetRequiredService<AuthDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            await DbInitializer.Initialize(appDbContext, authDbContext, userManager, roleManager);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Erro ao inicializar banco de dados do Identity");
            throw;
        }
    }
}
