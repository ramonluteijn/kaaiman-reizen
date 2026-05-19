using System.ComponentModel.DataAnnotations;

namespace Kaaiman_reizen.Data.Entities;

public class JourneyNotificationHistory
{
    public int Id { get; set; }

    [Required]
    public int JourneyId { get; set; }
    public Journey Journey { get; set; } = default!;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required]
    public int DaysBeforeStart { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
