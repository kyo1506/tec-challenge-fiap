using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos.Requests;

public class RefundPurchaseRequest
{
    [Required] public Guid UserId { get; set; }

    [Required] public Guid GameId { get; set; }
}