namespace TecChallenge.Shared.Models.Dtos.Responses;

public class RefundResponse
{
    public string GameName { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public decimal NewBalance { get; set; }
}