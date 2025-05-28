namespace TecChallenge.Domain.Notifications;

public class Notification(string message)
{
    public string Message { get; } = message;
}