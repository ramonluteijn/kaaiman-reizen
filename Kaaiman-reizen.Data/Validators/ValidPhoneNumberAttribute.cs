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

        var phoneNumberString = value.ToString()!;

        try
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            var phoneNumber = phoneUtil.Parse(phoneNumberString, _countryCode);
            var isValid = phoneUtil.IsValidNumber(phoneNumber);
            var numberType = phoneUtil.GetNumberType(phoneNumber);

            var isValidType = numberType == PhoneNumberType.MOBILE ||
                             numberType == PhoneNumberType.FIXED_LINE ||
                             numberType == PhoneNumberType.FIXED_LINE_OR_MOBILE;

            if (!isValid || !isValidType) return false;

            if (phoneNumberString.TrimStart().StartsWith("+"))
            {
                return true;
            }

            if (_countryCode == "NL")
            {
                return phoneNumberString.StartsWith("0");
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
