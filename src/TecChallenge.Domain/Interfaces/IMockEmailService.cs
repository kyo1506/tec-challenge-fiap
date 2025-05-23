namespace TecChallenge.Domain.Interfaces;

public interface IMockEmailService
{
    Task<bool> SendAsync(string subject, string body, string recipient, string env);
    Task<bool> CheckEmailServerHealthAsync();
}