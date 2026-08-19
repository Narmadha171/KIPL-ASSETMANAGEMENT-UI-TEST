using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.None)]

namespace KIPL.AssetManagement.UiTests;

public class BasePageTest : PageTest
{
    protected const string BaseUrl = "https://localhost:7218";

    [SetUp]
    public void BaseSetUp()
    {
        Page.SetDefaultTimeout(60000); // Increase timeout to 60 seconds
        Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
    }

    protected async Task LoginAsAsync(string email, string password = "Kipl@12345")
    {
        await Page.GotoAsync($"{BaseUrl}/Account/Login?ReturnUrl=%2F");
        
        // Fill in details and click submit
        await Page.FillAsync("input[name='Input.Email']", email);
        await Page.FillAsync("input[name='Input.Password']", password);
        await Page.ClickAsync("button[type='submit']");
        
        // Wait for page navigation after login
        await Page.WaitForURLAsync(url => !url.Contains("/Account/Login"));
    }

    protected async Task AssertAccessDeniedAsync(string relativeUrl)
    {
        await Page.GotoAsync($"{BaseUrl}{relativeUrl}");
        Assert.That(Page.Url.Contains("AccessDenied"), Is.True, $"Access to '{relativeUrl}' should be denied.");
    }

    protected async Task AssertPageHeaderAsync(string relativeUrl, string headerText)
    {
        await Page.GotoAsync($"{BaseUrl}{relativeUrl}");
        Assert.That(Page.Url.Contains("AccessDenied"), Is.False, $"Access to '{relativeUrl}' should be allowed.");
        var heading = Page.Locator("h1");
        await Expect(heading).ToContainTextAsync(headerText);
    }
}
