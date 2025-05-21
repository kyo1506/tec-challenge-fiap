using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class PromotionGameUpdateRequest
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [Range(1, 100)]
    public decimal DiscountPercentage { get; set; }
}