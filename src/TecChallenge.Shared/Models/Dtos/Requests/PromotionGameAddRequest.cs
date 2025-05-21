using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class PromotionGameAddRequest
{
    public Guid PromotionId { get; set; }
    
    [Required]
    public Guid GameId { get; set; }
    
    [Required]
    [Range(1, 100)]
    public decimal DiscountPercentage { get; set; }
}