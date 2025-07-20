using System.Net.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TecChallenge.Application.Extensions;

namespace TecChallenge.Application.Configurations;

public static class HealthChecksConfig
{
    private static readonly string[] SqlTags = ["sql-server"];
    private static readonly string[] MailTags = ["email-server"];

    public static void AddHealthChecksConfig(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services
            .AddHealthChecks()
            .AddNpgSql(
                connectionString ?? throw new InvalidDataException(),
                "SELECT 1",
                name: "Database",
                failureStatus: HealthStatus.Degraded,
                tags: SqlTags
            );
        
            // .AddCheck<MailHealthCheck>(
            //     "Mail Server",
            //     HealthStatus.Unhealthy,
            //     MailTags
            // );

        services
            .AddHealthChecksUI(options =>
            {
                options.UseApiEndpointHttpMessageHandler(sp =>
                {
                    return new HttpClientHandler
                    {
                        ClientCertificateOptions = ClientCertificateOption.Manual,
                        ServerCertificateCustomValidationCallback = (
                            httpRequestMessage,
                            cert,
                            cetChain,
                            policyErrors
                        ) => policyErrors == SslPolicyErrors.None
                    };
                });

                options.SetEvaluationTimeInSeconds(60);
                options.MaximumHistoryEntriesPerEndpoint(60);
                options.SetApiMaxActiveRequests(1);
                options.AddHealthCheckEndpoint(
                    "Tech Challenge Api",
                    configuration.GetValue<string>("UrlHealthCheck")
                    ?? throw new InvalidDataException()
                );
                options.DisableDatabaseMigrations();
            })
            .AddInMemoryStorage();
    }
}