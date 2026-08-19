using Microsoft.Playwright;
using NUnit.Framework;

namespace KIPL.AssetManagement.UiTests;

[TestFixture]
public class EmployeeTests : BasePageTest
{
    [Test]
    public async Task Employee_Verify_All_Features_In_Single_Session()
    {
        // 1. Log in once as general employee
        await LoginAsAsync("priya.shankar@kipl.com");

        // ==========================================
        // FEATURE AREA: Global Topbar Elements
        // ==========================================
        // A. Notification Bell Modal Test
        await Page.ClickAsync("#globalNotificationBtn");
        await Expect(Page.Locator("#modal-notificationsModal.open")).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-notificationsModal.open button[data-close-modal]");

        // B. Topbar Global Search Box (Visible for all layout users)
        await Expect(Page.Locator("input[placeholder*='Search assets']")).ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Page Navigation
        // ==========================================
        await AssertPageHeaderAsync("/Me/Assets", "My Assets");
        await AssertPageHeaderAsync("/Me/Requests", "My Requests");

        // Verify Dashboard Buttons
        await Page.GotoAsync(BaseUrl + "/Me/Dashboard");
        await Expect(Page.Locator("a:has-text('Return an asset')").First).ToBeVisibleAsync();
        await Expect(Page.Locator("a:has-text('Request an asset')").First).ToBeVisibleAsync();

        // Verify Sidebar Navigation Links for Employee
        await Page.ClickAsync("a.nav-item:has-text('Dashboard')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Me/Dashboard"));

        await Page.ClickAsync("a.nav-item:has-text('My Assets')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Assets");

        // Verify "Report an issue" modal opens
        var firstReportBtn = Page.Locator("button:has-text('Report an issue')").First;
        if (await firstReportBtn.IsVisibleAsync())
        {
            await firstReportBtn.ClickAsync();
            await Expect(Page.Locator("#modal-reportIssue.open")).ToBeVisibleAsync();
            await Page.ClickAsync("#modal-reportIssue button[data-close-modal]");
        }

        await Page.ClickAsync("a.nav-item:has-text('My Requests')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Requests");

        await Page.ClickAsync("a.nav-item:has-text('My Return')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Returns");

        // Assert Employee does NOT see Admin/Manager sidebar links
        var adminDashboardLink = Page.Locator("a.nav-item[href='/Ops/Dashboard']");
        var adminInventoryLink = Page.Locator("a.nav-item[href='/Ops/Inventory']");
        var teamApprovalsLink = Page.Locator("a.nav-item[href='/Team/Approvals']");
        await Expect(adminDashboardLink).Not.ToBeVisibleAsync();
        await Expect(adminInventoryLink).Not.ToBeVisibleAsync();
        await Expect(teamApprovalsLink).Not.ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Submit Request Flow
        // ==========================================
        await AssertPageHeaderAsync("/Me/Requests", "My Requests");
        // Click the request button
        await Page.ClickAsync("button:has-text('Request an asset')");
        var modalTitle = Page.Locator("h3:has-text('Request an asset')").First;
        await Expect(modalTitle).ToBeVisibleAsync();

        // Fill form fields
        string uniqueRequestItem = "iPad Pro - " + System.Guid.NewGuid().ToString().Substring(0, 8);
        await Page.FillAsync("input[id='Input_ItemName']", uniqueRequestItem);
        await Page.SelectOptionAsync("select[id='Input_Category']", new[] { "0" }); // 0 matches Laptop/Device
        await Page.FillAsync("textarea[id='Input_Reason']", "Need an iPad for traveling design demonstrations.");
        await Page.ClickAsync("button:has-text('Submit request')");

        // Verify the requested item appears in the list after refresh
        await Page.WaitForURLAsync(url => url.Contains("/Me/Requests"));
        await Expect(Page.Locator($"text={uniqueRequestItem}").First).ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Submit Return Flow
        // ==========================================
        await AssertPageHeaderAsync("/Me/Assets", "My Assets");
        // Click Return on first asset card if visible
        var returnLink = Page.Locator("a:has-text('Return')").First;
        if (await returnLink.IsVisibleAsync())
        {
            await returnLink.ClickAsync();
            
            // Verify redirected to My Returns page and modal opens
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Me/Returns.*"));
            var returnBtn = Page.Locator("button:has-text('Return an asset')");
            if (await returnBtn.IsVisibleAsync())
            {
                await returnBtn.ClickAsync();
                await Expect(Page.Locator("#modal-raiseReturn.open")).ToBeVisibleAsync();
                await Page.FillAsync("#modal-raiseReturn.open input[name='dropOffLocation']", "IT Support Desk, Level 1");
                await Page.FillAsync("#modal-raiseReturn.open textarea[name='reason']", "Upgrading laptop.");
                await Page.ClickAsync("#modal-raiseReturn.open button[type='submit']:has-text('Raise return')");
            }
        }

        // ==========================================
        // FEATURE AREA: Restricted Boundary Portals
        // ==========================================
        await AssertAccessDeniedAsync("/Ops/Dashboard");
        await AssertAccessDeniedAsync("/Ops/Inventory");
        await AssertAccessDeniedAsync("/Ops/Users");
        await AssertAccessDeniedAsync("/Ops/Roles");
        await AssertAccessDeniedAsync("/Ops/Audit");
        await AssertAccessDeniedAsync("/Ops/HrApprovals");
        await AssertAccessDeniedAsync("/Ops/ExecApprovals");
        await AssertAccessDeniedAsync("/Ops/ReturnTracking");
        await AssertAccessDeniedAsync("/Ops/ServiceQueue");
        await AssertAccessDeniedAsync("/Ops/BulkImport");
        await AssertAccessDeniedAsync("/Team/Approvals");
        await AssertAccessDeniedAsync("/Team/Assets");
    }
}
