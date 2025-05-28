using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using TecChallenge.Domain.Interfaces;

namespace TecChallenge.Infrastructure.Services;

public class EmailService(IConfiguration configuration) : IEmailService
{
    public async Task<bool> SendAsync(string subject, string body, string recipient, string env)
    {
        var emailMessage = CreateEmailMessage(subject, body, recipient, env);
        using var smtpClient = new SmtpClient();
        await ConnectAndAuthenticateAsync(smtpClient);
        await smtpClient.SendAsync(emailMessage);
        await smtpClient.DisconnectAsync(true);

        return true;
    }

    public async Task<bool> CheckEmailServerHealthAsync()
    {
        try
        {
            using var smtpClient = new SmtpClient();
            await ConnectAndAuthenticateAsync(smtpClient);
            await smtpClient.DisconnectAsync(true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private MimeMessage CreateEmailMessage(
        string subject,
        string body,
        string recipient,
        string env
    )
    {
        var emailMessage = new MimeMessage();
        var fromAddress = configuration["EmailConfiguration:Username"];
        var mailboxName =
            env == "Production" ? "Tech Challenge" : $"Tech Challenge - ({env})";

        emailMessage.From.Add(new MailboxAddress(mailboxName, fromAddress));
        emailMessage.To.Add(MailboxAddress.Parse(recipient));
        AddBccRecipients(emailMessage);
        emailMessage.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        emailMessage.Body = bodyBuilder.ToMessageBody();

        return emailMessage;
    }

    private void AddBccRecipients(MimeMessage emailMessage)
    {
        var bccAddresses =
            configuration["EmailConfiguration:Bcc"]
                ?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];

        foreach (var bccAddress in bccAddresses) emailMessage.Bcc.Add(MailboxAddress.Parse(bccAddress));
    }

    private async Task ConnectAndAuthenticateAsync(SmtpClient smtpClient)
    {
        var host = configuration["EmailConfiguration:Host"];
        var port = int.Parse(
            configuration["EmailConfiguration:Port"] ?? throw new InvalidDataException()
        );
        var username = configuration["EmailConfiguration:Username"];
        var password = configuration["EmailConfiguration:Password"];
        var enableSsl = bool.Parse(
            configuration["EmailConfiguration:EnableSSL"] ?? throw new InvalidDataException()
        );

        var secureOption = enableSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await smtpClient.ConnectAsync(host, port, secureOption);
        await smtpClient.AuthenticateAsync(username, password);
    }
}