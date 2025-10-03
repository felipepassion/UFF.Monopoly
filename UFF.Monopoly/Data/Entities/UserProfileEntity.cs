using System.ComponentModel.DataAnnotations;

namespace UFF.Monopoly.Data.Entities;

public class UserProfileEntity
{
    [Key]
    public Guid Id { get; set; }

    // Client identifier persisted in session (e.g., a GUID string)
    [MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public Guid? LastBoardId { get; set; }

    [MaxLength(256)]
    public string? PawnImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}