namespace TecChallenge.Domain.Interfaces;

public interface IEmailService
{
    Task<bool> SendAsync(string subject, string body, string recipient, string env);
    Task<bool> CheckEmailServerHealthAsync();
}