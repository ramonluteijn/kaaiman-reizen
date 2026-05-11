namespace Kaaiman_reizen.Data.Services;

public interface IEmailDispatcher
{
    Task SendEmailAsync(string email, string subject, string message);
    Task SendEmailToUsersAsync(List<string> emailAddresses, string subject, string message);
}
