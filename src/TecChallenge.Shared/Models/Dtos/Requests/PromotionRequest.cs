using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class PromotionRequest
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(
        100,
        ErrorMessage = "Name cannot be longer than 100 characters and less than 2 characters",
        MinimumLength = 2
    )]
    public string Name { get; set; } = string.Empty;

    [Required] public DateTime StartDate { get; set; }
    
    [Required] public DateTime EndDate { get; set; }

    [Required] public IEnumerable<PromotionGameRequest> GamesOnSale { get; set; } = [];
}