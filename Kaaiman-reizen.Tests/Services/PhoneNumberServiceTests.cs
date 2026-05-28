using Kaaiman_reizen.Data.Services;

namespace Kaaiman_reizen.Tests.Services;

public class PhoneNumberServiceTests
{
    // Only Dutch phone numbers are valid, so we will only test those
    [Fact]
    public void IsValidPhoneNumber_ValidNumber_ReturnsTrue_31()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("+31612345678"));
    }

    [Fact]
    public void IsValidPhoneNumber_ValidNumber_ReturnsTrue_06()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("0612345678"));
    }

    [Fact]
    public void IsValidPhoneNumber_ValidNumber_ReturnsTrue_31_Dash()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("+31-612345678"));
    }

    [Fact]
    public void IsValidPhoneNumber_ValidNumber_ReturnsTrue_06_Dash()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("06-12345678"));
    }

    [Fact]
    public void IsValidPhoneNumber_ValidNumber_ReturnsFalse_Short_31()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+3161234567"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Short_06()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("061234567"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Long_31()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+316123456789"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Long_06()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("06123456789"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Long_06_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("06-123456789"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Short_31()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+316-123456789"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Short_06_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("06-1234567"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Short_31_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+31-61234567"));
    }

    // test with wrong country number
    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_WrongCountryCode_Plus()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+4412345678"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_WrongCountryCode_Dash_Plus()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+44-12345678"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_WrongPhoneNumber()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("239884262"));
    }

    [Fact]
    public void sValidPhoneNumber_InvalidNumber_ReturnsFalse_WrongPhoneNumber_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("23-9884262"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Empty()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber(""));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Whitespace()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber(" "));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("-"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Dash_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("-D"));
    }

    [Fact]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse_Long()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+456789"));
    }

    [Fact]
    public void IsValidPhoneNumber_USNumber_ReturnsTrue()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("+12025550173", "US"));
    }

    [Fact]
    public void IsValidPhoneNumber_USNumber_NoPlus_ReturnsTrue()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("2025550173", "US"));
    }

    [Fact]
    public void IsValidPhoneNumber_BELumber_ReturnsTrue()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("+32470123456", "BE"));
    }

    [Fact]
    public void IsValidPhoneNumber_BENumber_NoPlus_ReturnsTrue()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("0470123456", "BE"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsTrue_Plus()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("+31412630453"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsTrue()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("0412630453"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsTrue_Dash()
    {
        var service = new PhoneNumberService();
        Assert.True(service.IsValidPhoneNumber("0412-630453"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsFalse_Short()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("041263045"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsFalse_Long()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("04126304534"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsFalse_Short_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("0412-63045"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsFalse_Long_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("0412-63045"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsFalse_WrongCountryCode()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+4412630453"));
    }

    [Fact]
    public void isValidLandlineNumber_ReturnsFalse_WrongCountryCode_Dash()
    {
        var service = new PhoneNumberService();
        Assert.False(service.IsValidPhoneNumber("+44-12630453"));
    }

    // formatphonenumber function tests
    [Fact]
    public void FormatPhoneNumber_ValidNumber_ReturnsFormatted_31()
    {
        var service = new PhoneNumberService();
        Assert.Equal("+31612345678", service.FormatPhoneNumber("0612345678"));
    }

    [Fact]
    public void FormatPhoneNumber_ValidNumber_ReturnsFormatted_06_Dash()
    {
        var service = new PhoneNumberService();
        Assert.Equal("+31612345678", service.FormatPhoneNumber("06-12345678"));
    }

    [Fact]
    public void FormatPhoneNumber_ValidNumber_ReturnsFormatted_31_Dash()
    {
        var service = new PhoneNumberService();
        Assert.Equal("+31612345678", service.FormatPhoneNumber("+31-612345678"));
    }

    [Fact]
    public void FormatPhoneNumber_InvalidNumber_ReturnsNull_Short_31()
    {
        var service = new PhoneNumberService();
        Assert.Null(service.FormatPhoneNumber("+3161234567"));
    }

    [Fact]
    public void FormatPhoneNumber_InvalidNumber_ReturnsNull_Short_06()
    {
        var service = new PhoneNumberService();
        Assert.Null(service.FormatPhoneNumber("061234567"));
    }

    [Fact]
    public void FormatPhoneNumber_InvalidNumber_ReturnsNull_Long()
    {
        var service = new PhoneNumberService();
        Assert.Null(service.FormatPhoneNumber("06123456789"));
    }

    [Fact]
    public void FormatPhoneNumber_InvalidNumber_ReturnsNull_Short_31_Dash()
    {
        var service = new PhoneNumberService();
        Assert.Null(service.FormatPhoneNumber("06-123456789"));
    }

    [Fact]
    public void FormatPhoneNumber_InvalidNumber_ReturnsNull_Long_06()
    {
        var service = new PhoneNumberService();
        Assert.Null(service.FormatPhoneNumber("06-123456789"));
    }

    [Fact]
    public void FormatPhoneNumber_InvalidNumber_ReturnsNull_Short_06_Dash()
    {
        var service = new PhoneNumberService();
        Assert.Null(service.FormatPhoneNumber("06-123456789"));
    }

    [Fact]
    public void FormatPhoneNumber_InvalidNumber_ReturnsNull_Long_31_Dash()
    {
        var service = new PhoneNumberService();
        Assert.Null(service.FormatPhoneNumber("+31-6123456789"));
    }

    // getcountrycode function tests
    [Fact]
    public void GetCountryCode_ValidNumber_ReturnsCountryCode_31()
    {
        var service = new PhoneNumberService();
        Assert.Equal(31, service.GetCountryCode("+31612345678"));
    }

    [Fact]
    public void GetCountryCode_ValidNumber_ReturnsCountryCode_06()
    {
        var service = new PhoneNumberService();
        Assert.Equal(31, service.GetCountryCode("0612345678"));
    }

    [Fact]
    public void GetCountryCode_ValidNumber_ReturnsCountryCode_06_Dash()
    {
        var service = new PhoneNumberService();
        Assert.Equal(31, service.GetCountryCode("06-12345678"));
    }

    [Fact]
    public void GetCountryCode_ValidNumber_ReturnsCountryCode_31_Dash()
    {
        var service = new PhoneNumberService();
        Assert.Equal(31, service.GetCountryCode("+31-612345678"));
    }
}