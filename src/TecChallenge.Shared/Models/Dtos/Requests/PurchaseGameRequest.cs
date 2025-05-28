using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class PurchaseGameRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid GameId { get; set; }
    
    public Guid? PromotionGameId { get; set; }
}