using TecChallenge.Domain.Interfaces;

namespace TecChallenge.Infrastructure.Services;

public class MockEmailService : IMockEmailService
{
    private List<MockEmail> SentEmails { get; } = [];
    private bool IsHealthy { get; set; } = true;

    public Task<bool> SendAsync(string subject, string body, string recipient, string env)
    {
        SentEmails.Add(new MockEmail(subject, body, recipient, env));
        return Task.FromResult(true);
    }

    public Task<bool> CheckEmailServerHealthAsync()
    {
        return Task.FromResult(IsHealthy);
    }
}

public record MockEmail(string Subject, string Body, string Recipient, string Env);