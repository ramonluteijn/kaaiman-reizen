using System.ComponentModel.DataAnnotations;
using PhoneNumbers;

namespace Kaaiman_reizen.Data.Validators;

/// <summary>
/// Validates that a phone number is valid for the configured country.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidPhoneNumberAttribute : ValidationAttribute
{
    private readonly string _countryCode;
    private const string DefaultCountryCode = "NL";


    public ValidPhoneNumberAttribute(string countryCode = DefaultCountryCode)
    {
        _countryCode = countryCode;
        ErrorMessage = "Vul een geldig telefoonnummer in.";
    }

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return true; // Null/empty is handled by [Required] attribute
        }

        try
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            var phoneNumber = phoneUtil.Parse(value.ToString(), _countryCode);
            return phoneUtil.IsValidNumber(phoneNumber);
        }
        catch
        {
            return false;
        }
    }
}
