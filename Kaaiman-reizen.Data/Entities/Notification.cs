using Kaaiman_reizen.Data.Identity;
using System.ComponentModel.DataAnnotations;

namespace Kaaiman_reizen.Data.Entities;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = default!;
}
