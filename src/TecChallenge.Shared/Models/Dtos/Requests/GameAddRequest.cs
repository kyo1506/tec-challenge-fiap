using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class GameAddRequest
{
    [Required]
    [StringLength(
        100,
        ErrorMessage = "Name cannot be longer than 100 characters and less than 2 characters",
        MinimumLength = 2
    )]
    public string Name { get; set; } = string.Empty;

    [Required] public decimal Price { get; set; }
}