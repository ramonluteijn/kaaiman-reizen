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
            return _phoneUtil.IsValidNumber(parsedNumber);
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
            if (!_phoneUtil.IsValidNumber(parsedNumber))
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
            if (!_phoneUtil.IsValidNumber(parsedNumber))
                return null;

            return parsedNumber.CountryCode;
        }
        catch
        {
            return null;
        }
    }

    private static bool CheckWhiteSpace(string phoneNumber)
    {
        return string.IsNullOrWhiteSpace(phoneNumber);
    }
}
