namespace Kaaiman_reizen.Helpers;

public static class DateDisplay
{
    public const string DateFormat = "dd-MM-yyyy";
    public const string MonthYearFormat = "MM-yyyy";
    public const string DateTimeFormat = "dd-MM-yyyy HH:mm";

    public static string FormatDate(DateOnly date) => date.ToString(DateFormat);

    public static string FormatDate(DateTime dateTime) => dateTime.ToString(DateFormat);

    public static string FormatMonthYear(DateOnly date) => date.ToString(MonthYearFormat);

    public static string FormatDateRange(DateOnly start, DateOnly end) => $"{FormatDate(start)} - {FormatDate(end)}";

    public static string FormatDateTime(DateTime dateTime) => dateTime.ToString(DateTimeFormat);

    public static DateTime ToUserLocal(DateTime utcDateTime, int timezoneOffsetMinutes)
    {
        var utc = utcDateTime.Kind switch
        {
            DateTimeKind.Utc => utcDateTime,
            DateTimeKind.Local => utcDateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
        };

        return utc.AddMinutes(-timezoneOffsetMinutes);
    }
}