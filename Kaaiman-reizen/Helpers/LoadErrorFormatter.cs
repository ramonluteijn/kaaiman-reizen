using Microsoft.Extensions.Hosting;

namespace Kaaiman_reizen.Helpers;

/// <summary>Builds readable load-failure text for UI (and pairs with ILogger for full diagnostics).</summary>
public static class LoadErrorFormatter
{
    /// <summary>
    /// Chains <see cref="Exception.Message"/> from the exception and inner exceptions.
    /// In Development, appends the stack trace so local debugging is easier.
    /// </summary>
    public static string Format(Exception ex, IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var parts = new List<string>();
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            var msg = current.Message.Trim();
            if (msg.Length > 0 && !parts.Contains(msg, StringComparer.Ordinal))
                parts.Add(msg);
        }

        var summary = parts.Count > 0
            ? string.Join(" → ", parts)
            : ex.GetType().Name;

        if (environment?.IsDevelopment() == true && !string.IsNullOrWhiteSpace(ex.StackTrace))
            return summary + Environment.NewLine + Environment.NewLine + ex.StackTrace.Trim();

        return summary;
    }
}
