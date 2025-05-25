using System.Text.Json.Serialization;
using Asp.Versioning;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using TecChallenge.Application.Extensions;

namespace TecChallenge.Application.Configurations;

public static class ApiConfiguration
{
    public static void AddApiConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler
                    .IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition
                    .WhenWritingNull;
            });

        services
            .AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

        services.Configure<UrlConfiguration>(options =>
        {
            options.UrlPortal =
                configuration.GetValue<string>("UrlPortal")
                ?? "https://localhost:5000";
        });

        services.AddCors(options =>
        {
            options.AddPolicy(
                "Development",
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );
        });
    }

    public static void UseApiConfig(this IApplicationBuilder app,
        IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseCors("Development");
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseCors();
        }

        app.UseHsts();

        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Information;
        });

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseStaticFiles();

        app.UseExceptionHandler();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            endpoints.MapHealthChecks(
                "/api/health",
                new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                }
            );

            endpoints.MapHealthChecksUI(setup => { setup.UIPath = "/api/monitor"; });
        });
    }
}