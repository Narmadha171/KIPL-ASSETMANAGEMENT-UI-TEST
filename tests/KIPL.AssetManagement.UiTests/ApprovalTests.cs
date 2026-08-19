using Microsoft.Playwright;
using NUnit.Framework;

namespace KIPL.AssetManagement.UiTests;

[TestFixture]
public class ApprovalTests : BasePageTest
{
    [Test]
    public async Task HR_Verify_All_Features_In_Single_Session()
    {
        // 1. Log in once as HR Specialist
        await LoginAsAsync("amanda.lee@kipl.com");
        
        // ==========================================
        // FEATURE AREA: Global Topbar Elements
        // ==========================================
        // A. Notification Bell Modal Test
        await Page.ClickAsync("#globalNotificationBtn");
        await Expect(Page.Locator("#modal-notificationsModal.open")).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-notificationsModal.open button[data-close-modal]");

        // B. Topbar Global Search Box
        await Page.FillAsync("input[placeholder*='Search assets']", "Dell");
        await Page.PressAsync("input[placeholder*='Search assets']", "Enter");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));

        // ==========================================
        // FEATURE AREA: Dashboard Navigation & Tab check
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Dashboard", "Operations overview");
        
        // HR can touch the "PENDING APPROVAL" card to go to HrApprovals page
        await Page.ClickAsync("a.kpi-card:has-text('PENDING APPROVAL')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/HrApprovals"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // HR can touch the "IN STOCK" card
        await Page.ClickAsync("a.kpi-card:has-text('IN STOCK')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // HR can touch the "LOST" card
        await Page.ClickAsync("a.kpi-card:has-text('LOST')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // Check Dashboard tabs
        await Page.ClickAsync("a.tab:has-text('Returns')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Returns"));

        await Page.ClickAsync("a.tab:has-text('Recent activity')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Activity"));

        await Page.ClickAsync("a.tab:has-text('Open requests')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Requests"));

        // ==========================================
        // FEATURE AREA: HR Approvals Page & Online Orders
        // ==========================================
        await AssertPageHeaderAsync("/Ops/HrApprovals", "Approvals");
        
        // 1. Approve Michael Chen's Webcam request if present
        var webcamCard = Page.Locator("div.panel:has-text('Webcam')").First;
        if (await webcamCard.IsVisibleAsync())
        {
            await webcamCard.Locator("button:has-text('Approve')").ClickAsync();
        }

        // 2. Verify submission on Sarah Jenkins's Dell U2723QE Monitor if present
        var verifyBtn = Page.Locator("button:has-text('Verify submission')").First;
        if (await verifyBtn.IsVisibleAsync())
        {
            await verifyBtn.ClickAsync();
            await Expect(Page.Locator("#modal-verifySubmission.open")).ToBeVisibleAsync();
            await Page.FillAsync("#modal-verifySubmission.open input[name='SerialNumber']", "SN-Verify-12345");
            await Page.ClickAsync("#modal-verifySubmission.open button[type='submit']");
        }

        // 3. Navigate to My Requests page, cancel Amanda's pending request if present
        await AssertPageHeaderAsync("/Me/Requests", "My Requests");
        var headsetCard = Page.Locator(".req-card:has-text('Noise-cancelling Headset')").First;
        if (await headsetCard.IsVisibleAsync() && await headsetCard.Locator("button:has-text('Cancel request')").IsVisibleAsync())
        {
            await headsetCard.Locator("button:has-text('Cancel request')").ClickAsync();
        }

        // 4. Navigate to My Assets page, check Report issue modal
        await AssertPageHeaderAsync("/Me/Assets", "My Assets");
        var lampCard = Page.Locator(".asset-card:has-text('Desk Lamp')").First;
        if (await lampCard.IsVisibleAsync())
        {
            await lampCard.Locator("button:has-text('Report an issue')").ClickAsync();
            await Expect(Page.Locator(".modal-overlay.open")).ToBeVisibleAsync();
            await Page.ClickAsync(".modal-overlay.open button[data-close-modal]");
        }

        // ==========================================
        // FEATURE AREA: Inventory Search & Action Button Hiding Verification
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Inventory", "Inventory");
        
        // Search bar & dropdown filters on Inventory
        await Page.FillAsync("input[placeholder*='Search assets']", "Dell");
        await Page.PressAsync("input[placeholder*='Search assets']", "Enter");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));

        // Interact with filter search space and select dropdown options
        await Page.FillAsync("input[id='Search']", "Latitude");
        await Page.SelectOptionAsync("select[id='Category']", new[] { "0" }); // Laptops
        await Page.SelectOptionAsync("select[id='Status']", new[] { "1" });   // Assigned
        await Page.SelectOptionAsync("select[id='Condition']", new[] { "0" }); // New
        
        // Reset filters back for further tests
        await Page.FillAsync("input[id='Search']", "");
        await Page.SelectOptionAsync("select[id='Category']", new[] { "" });
        await Page.SelectOptionAsync("select[id='Status']", new[] { "" });
        await Page.SelectOptionAsync("select[id='Condition']", new[] { "" });

        // Assert HR does NOT see "Add asset" or "Bulk import" buttons (since HR cannot manage inventory)
        var addAssetBtn = Page.Locator("button:has-text('Add asset')");
        var bulkImportLink = Page.Locator("a:has-text('Bulk import')");
        await Expect(addAssetBtn).Not.ToBeVisibleAsync();
        await Expect(bulkImportLink).Not.ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Sidebar Links & Security Gates
        // ==========================================
        await Page.ClickAsync("a.nav-item:has-text('Dashboard')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Operations overview");

        await Page.ClickAsync("a.nav-item:has-text('Inventory')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Inventory");

        await Page.ClickAsync("a.nav-item:has-text('Approvals')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Approvals");

        await Page.ClickAsync("a.nav-item:has-text('My Assets')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Assets");

        await Page.ClickAsync("a.nav-item:has-text('My Requests')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Requests");

        await Page.ClickAsync("a.nav-item:has-text('My Return')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Returns");

        // HR is restricted from admin settings, service queues, returns, and security trails
        await AssertAccessDeniedAsync("/Ops/Users");
        await AssertAccessDeniedAsync("/Ops/Roles");
        await AssertAccessDeniedAsync("/Ops/Audit");
        await AssertAccessDeniedAsync("/Ops/ServiceQueue");
        await AssertAccessDeniedAsync("/Ops/ReturnTracking");
        await AssertAccessDeniedAsync("/Ops/BulkImport");
    }

    [Test]
    public async Task Manager_Verify_All_Features_In_Single_Session()
    {
        // 1. Log in once as Reporting Manager
        await LoginAsAsync("michael.chen@kipl.com");
        
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
        // FEATURE AREA: Allowed Pages & Lists
        // ==========================================
        await AssertPageHeaderAsync("/Team/Approvals", "Team Approvals");

        // Verify Reject request card with reason modal if present in pending list
        var macbookCard = Page.Locator("div.panel:has(button:has-text('Reject')):has-text('MacBook Pro 16\"')").First;
        if (await macbookCard.IsVisibleAsync())
        {
            await macbookCard.Locator("button:has-text('Reject')").ClickAsync();
            await Expect(Page.Locator("#modal-rejectRequest.open")).ToBeVisibleAsync();
            await Page.FillAsync("#modal-rejectRequest.open textarea[id='teamRejectReason']", "Needs manager approval detail review.");
            await Page.ClickAsync("#modal-rejectRequest.open button[type='submit']:has-text('Reject request')");
        }
        
        // Verify Team Assets list displays team members
        await AssertPageHeaderAsync("/Team/Assets", "Team assets");
        await Expect(Page.Locator("h3:has-text('Sarah Jenkins')")).ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Sidebar Navigation Links Check
        // ==========================================
        await Page.ClickAsync("a.nav-item:has-text('Dashboard')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Me/Dashboard"));

        await Page.ClickAsync("a.nav-item:has-text('Team Approvals')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Team Approvals");

        await Page.ClickAsync("a.nav-item:has-text('Team Assets')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Team assets");

        await Page.ClickAsync("a.nav-item:has-text('My Assets')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Assets");

        await Page.ClickAsync("a.nav-item:has-text('My Requests')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Requests");

        await Page.ClickAsync("a.nav-item:has-text('My Return')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Returns");

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
    }
}
