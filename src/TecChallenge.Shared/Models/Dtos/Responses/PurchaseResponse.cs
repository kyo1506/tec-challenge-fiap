using System;

namespace TecChallenge.Shared.Models.Dtos.Responses;

public class PurchaseResponse
{
    public Guid UserId { get; set; }
    public string? GameName { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? Price { get; set; }
    public decimal? Balance { get; set; }
}