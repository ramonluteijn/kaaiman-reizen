namespace Kaaiman_reizen.Data.Services;

/// <summary>
/// Service for validating and formatting phone numbers using libphonenumber.
/// </summary>
public interface IPhoneNumberService
{
    /// <summary>
    /// Validates a phone number for a given country.
    /// </summary>
    /// <param name="phoneNumber">The phone number to validate</param>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g., "NL" for Netherlands)</param>
    /// <returns>True if the phone number is valid, otherwise false</returns>
    bool IsValidPhoneNumber(string phoneNumber, string countryCode = "NL");

    /// <summary>
    /// Formats a phone number in international format.
    /// </summary>
    /// <param name="phoneNumber">The phone number to format</param>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g., "NL" for Netherlands)</param>
    /// <returns>The formatted phone number in E.164 format, or null if invalid</returns>
    string? FormatPhoneNumber(string phoneNumber, string countryCode = "NL");

    /// <summary>
    /// Gets the country code from a phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number</param>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g., "NL" for Netherlands)</param>
    /// <returns>The country code of the phone number, or null if invalid</returns>
    int? GetCountryCode(string phoneNumber, string countryCode = "NL");
}
