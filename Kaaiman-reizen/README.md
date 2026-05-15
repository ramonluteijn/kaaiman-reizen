# Structure

```
├── Properties             # Project properties and settings
├── wwwroot                # Static files (if applicable)
│   ├── css                # CSS files
│   ├── js                 # JavaScript files
│   ├── images             # Image files
│   └── lib                # Third-party libraries
├── Components             # Blazor components (if applicable)
│   ├── Layouts            # Layout components
│   ├── Pages              # Page components
│   └── Shared             # Shared components
├── Views                  # MVC views (if applicable)
│   ├── Home               # Views for the Home controller (example)
├── ViewModels             # View models for MVC or Blazor (if applicable)
│   └── Home               # View models for the Home controller (example)
├── Controllers            # MVC controllers (if applicable)
├── appsettings.json       # Application configuration file
├── Program.cs             # Application entry point
```

## Local development: database connection (User Secrets)

This app expects a connection string named `DefaultConnection`.

Recommended: configure it locally via **User Secrets** so passwords don’t end up in git.

From the repo root:

```bash
cd Kaaiman-reizen
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=kaaiman_reizen;Uid=root;Pwd=;{IF YOU HAVE PASSWORD OTHERWISE KEEP IT EMPTY ;) }"
```

After that you can run migrations using:

```bash
dotnet ef database update --project Kaaiman-reizen.Data --startup-project Kaaiman-reizen --context MainContext
```

## Email System

The application is configured to send emails using a dual-mode approach based on the environment:

### Development Environment

When running locally in Development mode (`if (builder.Environment.IsDevelopment())`), the application uses a **`ConsoleEmailSender`**.

- Emails are **not** genuinely sent to real inboxes.
- Instead, the content, subject, and recipient address are directly logged to the Visual Studio output console/terminal. Look for the `===== DUMMY EMAIL VERZONDEN NAAR =====` banners in your console to verify email dispatches.
- No real SMTP configuration is required for local testing of application flow.

### Production / Production-Like Environments

For environments that are not "Development", the system automatically registers the **`SmtpEmailSender`** and utilizes real SMTP network transport.

- You must supply legitimate `.NET User Secrets` or deployment environment variables under the `SmtpSettings` section to avoid crashes upon email dispatch operations.
- The `SmtpSettings` section requires the following schema:

```json
"SmtpSettings": {
  "Host": "smtp.yourprovider.com",
  "Port": 587,
  "Username": "your_smtp_username",
  "Password": "your_smtp_password",
  "SenderEmail": "no-reply@kaaiman-reizen.nl",
  "SenderName": "Kaaiman Reizen"
}
```

### Dispatching Emails In Code

The core of the email logic relies on `IEmailDispatcher`. If you want to send an email programmatically within the services:

1. Inject `IEmailDispatcher`.
2. Await `SendEmailToUsersAsync(emailAddresses, subject, message)` or `SendEmailAsync(email, subject, message)`.

## Journey Reminder Notifications

The application automatically sends email and dashboard notifications to travel leaders 7 days and 3 days before a journey starts.

### How It Works

1. **Background Service**: `JourneyReminderHostedService` runs periodically (configured interval) and checks for upcoming journeys
2. **Email Notifications**: Travel leaders receive emails with the journey name and start date
3. **Dashboard Notifications**: Notifications also appear on the travel leader's dashboard (if their email matches an ApplicationUser account)
4. **Duplicate Prevention**: `JourneyNotificationHistory` table tracks sent notifications to prevent duplicates

### Configuration

In `appsettings.Development.json` or `appsettings.json`:

```json
"JourneyReminder": {
  "IntervalSeconds": 86400
}
```

- **`IntervalSeconds`**: How often the background service checks for upcoming journeys
  - `10` = every 10 seconds (testing)
  - `60` = every 1 minute (testing)
  - `86400` = every 24 hours (1 day, production default)

### Development Testing

To test quickly during development:

1. Set `IntervalSeconds` to a small value (e.g., `10` seconds)
2. Create a journey with a start date that is exactly 3 or 7 days from today
3. Assign a travel leader whose email matches an ApplicationUser account
4. Run the app and monitor logs for "JourneyReminderHostedService"
5. Check:
   - Console output for email logs (in Development, emails log to console)
   - Dashboard for new notifications
   - Database `Notifications` table for new records

### Example Configuration for Testing

```json
"JourneyReminder": {
  "IntervalSeconds": 10
}
```

This will run the notification check every 10 seconds, perfect for quick testing.

### Database Tables

- **`Notifications`**: Stores all notifications displayed on the dashboard (created by `NotificationService.CreateNotificationAsync`)
- **`JourneyNotificationHistory`**: Tracks which notifications have been sent to prevent duplicates
