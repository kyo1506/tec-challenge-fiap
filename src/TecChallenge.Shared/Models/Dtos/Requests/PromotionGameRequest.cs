using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class PromotionGameRequest
{
    [Required]
    public Guid PromotionId { get; set; }
    
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    public decimal DiscountPercentage { get; set; }
}