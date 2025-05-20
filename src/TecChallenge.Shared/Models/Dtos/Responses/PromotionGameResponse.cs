using System;

namespace TecChallenge.Shared.Models.Dtos.Responses;

public class PromotionGameResponse
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public Guid GameId { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? GameName { get; set; }
    public string? PromotionName { get; set; }
}