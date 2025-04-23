using Microsoft.Extensions.Diagnostics.HealthChecks;
using TecChallenge.Domain.Interfaces;

namespace TecChallenge.Application.Extensions;

public class MailHealthCheck(IEmailService emailService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var isHealthy = await emailService.CheckEmailServerHealthAsync();

        return isHealthy
            ? HealthCheckResult.Healthy("Email server is reachable.")
            : HealthCheckResult.Unhealthy("Email server is unreachable.");
    }
}