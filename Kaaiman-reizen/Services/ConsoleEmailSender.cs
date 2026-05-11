using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Kaaiman_reizen.Services
{
    public class ConsoleEmailSender : IEmailSender, IEmailSender<Data.Identity.ApplicationUser>
    {
        private readonly ILogger<ConsoleEmailSender> _logger;

        public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation("===============================================");
            _logger.LogInformation("DUMMY EMAIL VERZONDEN NAAR: {Email}", email);
            _logger.LogInformation("ONDERWERP: {Subject}", subject);
            _logger.LogInformation("BERICHT: {Message}", htmlMessage);
            _logger.LogInformation("===============================================");
            return Task.CompletedTask;
        }

        public Task SendConfirmationLinkAsync(Data.Identity.ApplicationUser user, string email, string confirmationLink)
            => SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        public Task SendPasswordResetLinkAsync(Data.Identity.ApplicationUser user, string email, string resetLink)
            => SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

        public Task SendPasswordResetCodeAsync(Data.Identity.ApplicationUser user, string email, string resetCode)
            => SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
    }
}
