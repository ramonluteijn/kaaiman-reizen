using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace Kaaiman_reizen.Tests.Pages;
public class HomePageTests : PageTest
{
    private static readonly string ScreenshotDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "screenshots");

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
    };

    [Fact]
    public async Task ReisleidersBeheren_Button_IsVisible_WhenNoTravelLeadersExist()
    {
        Directory.CreateDirectory(ScreenshotDir);

        await AuthenticatedUser.LoginAsPlannerAsync(Page);
        await Page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDir, "01_home_after_login.png") });

        var button = Page.GetByRole(AriaRole.Link, new() { Name = "Reisleiders beheren" });
        await Expect(button).ToBeVisibleAsync();
        await Page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDir, "02_button_visible.png") });
    }

    /// <summary>
    /// Requires: journeys exist for the current year that have no published planning assignment.
    /// This is the default state of the dev-seeded database.
    /// </summary>
    [Fact]
    public async Task ReisplanningCompleet_IsNotVisible_WhenPlanningIsIncomplete()
    {
        Directory.CreateDirectory(ScreenshotDir);

        await AuthenticatedUser.LoginAsPlannerAsync(Page);

        var modal = Page.GetByText("Reisplanning compleet");
        await Expect(modal).Not.ToBeVisibleAsync();
        await Page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDir, "03_no_planning_complete_modal.png") });
    }

    /// <summary>
    /// Requires: a published planning exists where every journey for the current year
    /// has at least one travel leader assigned.
    /// </summary>
    [Fact]
    public async Task ReisplanningCompleet_IsVisible_WhenAllJourneysHaveLeaders()
    {
        Directory.CreateDirectory(ScreenshotDir);

        await AuthenticatedUser.LoginAsPlannerAsync(Page);

        var modal = Page.GetByText("Reisplanning compleet");
        await Expect(modal).ToBeVisibleAsync();
        await Page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDir, "04_planning_complete_modal.png") });
    }
}
