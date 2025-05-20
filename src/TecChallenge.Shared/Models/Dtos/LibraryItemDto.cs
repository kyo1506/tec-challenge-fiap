using System;
using System.ComponentModel.DataAnnotations;

namespace TecChallenge.Shared.Models.Dtos;

public class LibraryItemDto
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserLibraryId { get; set; }
    
    [Required]
    public Guid GameId { get; set; }
    
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public decimal PurchasePrice { get; set; }
}