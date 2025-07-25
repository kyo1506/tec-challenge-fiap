using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class PromotionUpdateRequest
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
