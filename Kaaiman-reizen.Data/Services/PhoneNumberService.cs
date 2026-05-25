using PhoneNumbers;

namespace Kaaiman_reizen.Data.Services;

public class PhoneNumberService : IPhoneNumberService
{
    private readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();
    private const string DefaultCountryCode = "NL";

    public bool IsValidPhoneNumber(string phoneNumber, string countryCode = DefaultCountryCode)
    {
        if (CheckWhiteSpace(phoneNumber)) return false;

        try
        {
            var parsedNumber = _phoneUtil.Parse(phoneNumber, countryCode);
            return IsValidParsedNumber(parsedNumber, phoneNumber, countryCode);
        }
        catch
        {
            return false;
        }
    }

    public string? FormatPhoneNumber(string phoneNumber, string countryCode = DefaultCountryCode)
    {
        if (CheckWhiteSpace(phoneNumber)) return null;

        try
        {
            var parsedNumber = _phoneUtil.Parse(phoneNumber, countryCode);
            if (!IsValidParsedNumber(parsedNumber, phoneNumber, countryCode))
                return null;

            return _phoneUtil.Format(parsedNumber, PhoneNumberFormat.E164);
        }
        catch
        {
            return null;
        }
    }

    public int? GetCountryCode(string phoneNumber, string countryCode = DefaultCountryCode)
    {
        if (CheckWhiteSpace(phoneNumber)) return null;

        try
        {
            var parsedNumber = _phoneUtil.Parse(phoneNumber, countryCode);
            if (!IsValidParsedNumber(parsedNumber, phoneNumber, countryCode))
                return null;

            return parsedNumber.CountryCode;
        }
        catch
        {
            return null;
        }
    }

    private bool IsValidParsedNumber(PhoneNumber parsedNumber, string originalPhoneNumber, string countryCode)
    {
        var isValid = _phoneUtil.IsValidNumber(parsedNumber);
        var numberType = _phoneUtil.GetNumberType(parsedNumber);

        var isValidType = numberType == PhoneNumberType.MOBILE ||
                         numberType == PhoneNumberType.FIXED_LINE ||
                         numberType == PhoneNumberType.FIXED_LINE_OR_MOBILE;

        if (!isValid || !isValidType) return false;

        if (originalPhoneNumber.TrimStart().StartsWith("+"))
        {
            return true;
        }

        if (countryCode == "NL")
        {
            return originalPhoneNumber.StartsWith("0");
        }

        return true;
    }

    private static bool CheckWhiteSpace(string phoneNumber)
    {
        return string.IsNullOrWhiteSpace(phoneNumber);
    }
}
