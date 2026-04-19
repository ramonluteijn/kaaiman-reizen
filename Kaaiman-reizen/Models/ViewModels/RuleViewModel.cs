namespace Kaaiman_reizen.Models.ViewModels;

public class RuleViewModel
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public string Description { get; set; } = null!;
    public object? TypedValue { get; set; }
    public bool IsActive { get; set; }
}