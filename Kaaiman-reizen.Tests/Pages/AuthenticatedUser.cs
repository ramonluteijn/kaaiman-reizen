using Microsoft.Playwright;

namespace Kaaiman_reizen.Tests.Pages;

public static class AuthenticatedUser
{
    public const string BaseUrl = "http://localhost:5002";

    public static Task LoginAsPlannerAsync(IPage page) =>
        LoginAsync(page, "planner@kaaiman.nl", "Kaaiman26!");

    public static Task LoginAsReisleiderAsync(IPage page) =>
        LoginAsync(page, "reisleider@kaaiman.nl", "Kaaiman26!");

    private static async Task LoginAsync(IPage page, string email, string password)
    {
        await page.GotoAsync($"{BaseUrl}/Account/Login");
        await page.FillAsync("input[name='Input.Email']", email);
        await page.FillAsync("input[name='Input.Password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForURLAsync($"{BaseUrl}/");
    }
}