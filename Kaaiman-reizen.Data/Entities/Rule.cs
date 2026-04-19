using System.ComponentModel.DataAnnotations.Schema;

namespace Kaaiman_reizen.Data.Entities;

public class Rule
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public string Description { get; set; } = null!;
    public string? Value { get; set; }

    [NotMapped]
    public object? TypedValue
    {
        get => int.TryParse(Value, out int number) ? number : Value;
        set => Value = value switch
        {
            null => null,
            int number => number.ToString(),
            string text => text,
            _ => throw new ArgumentException("TypedValue only supports int or string.")
        };
    }

    public bool IsActive { get; set; }
}
