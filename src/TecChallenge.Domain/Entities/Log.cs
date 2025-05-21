namespace TecChallenge.Domain.Entities;

public class Log
{
    public long Id { get; set; }
    public string? ApplicationName { get; set; }
    public string? Message { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Level { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
}