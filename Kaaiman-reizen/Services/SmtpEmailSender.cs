using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Kaaiman_reizen.Data.Identity;

namespace Kaaiman_reizen.Services;

public class SmtpEmailSender : IEmailSender, IEmailSender<ApplicationUser>
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        if (string.IsNullOrEmpty(_settings.Host))
        {
            _logger.LogWarning("Email was not sent because SMTP settings are missing.");
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = true
            };

            var senderEmail = !string.IsNullOrEmpty(_settings.SenderEmail) ? _settings.SenderEmail : _settings.Username;
            var fromAddress = new MailAddress(senderEmail, _settings.SenderName);

            using var message = new MailMessage
            {
                From = fromAddress,
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {email}", email);
        }
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        throw new NotImplementedException();
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        throw new NotImplementedException();
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        throw new NotImplementedException();
    }
}
