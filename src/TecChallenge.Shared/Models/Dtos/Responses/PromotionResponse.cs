using System;
using System.Collections.Generic;

namespace TecChallenge.Shared.Models.Dtos.Responses;

public class PromotionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public IEnumerable<PromotionGameResponse> GamesOnSale { get; set; } = [];
}