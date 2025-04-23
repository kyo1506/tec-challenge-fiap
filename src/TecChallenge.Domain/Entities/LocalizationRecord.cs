namespace TecChallenge.Domain.Entities;

public class LocalizationRecord : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string LocalizationCulture { get; set; } = string.Empty;
    public string ResourceKey { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}