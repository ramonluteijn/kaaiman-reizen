using Kaaiman_reizen.Data.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Kaaiman_reizen.Services;

public class EmailDispatcher : IEmailDispatcher
{
    private readonly IEmailSender _emailSender;

    public EmailDispatcher(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendEmailAsync(string email, string subject, string message)
    {
        return _emailSender.SendEmailAsync(email, subject, message);
    }

    public async Task SendEmailToUsersAsync(List<string> emailAddresses, string subject, string message)
    {
        foreach (var email in emailAddresses)
        {
            await _emailSender.SendEmailAsync(email, subject, message);
        }
    }
}
