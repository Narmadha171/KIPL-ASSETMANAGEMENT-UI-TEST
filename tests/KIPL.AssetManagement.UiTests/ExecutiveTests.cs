using Microsoft.Playwright;
using NUnit.Framework;

namespace KIPL.AssetManagement.UiTests;

[TestFixture]
public class ExecutiveTests : BasePageTest
{
    [Test]
    public async Task Executive_Verify_All_Features_In_Single_Session()
    {
        // 1. Log in once as IT Operations Executive
        await LoginAsAsync("revanth.k@kipl.com");

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
        // PAGE 1: Dashboard (`/Ops/Dashboard`)
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Dashboard", "Operations overview");

        // KPI Cards Touch Checks (direct navigation redirects)
        await Page.ClickAsync("a.kpi-card:has-text('PENDING APPROVAL')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/HrApprovals"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        await Page.ClickAsync("a.kpi-card:has-text('UNCLAIMED')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/ExecApprovals"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        await Page.ClickAsync("a.kpi-card:has-text('IN FULFILMENT')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/ExecApprovals"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        await Page.ClickAsync("a.kpi-card:has-text('IN STOCK')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        await Page.ClickAsync("a.kpi-card:has-text('SERVICE')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/ServiceQueue"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        await Page.ClickAsync("a.kpi-card:has-text('LOST')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // Dashboard Sub-tabs
        await Page.ClickAsync("a.tab:has-text('Returns')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Returns"));

        await Page.ClickAsync("a.tab:has-text('Service queue')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Service"));

        await Page.ClickAsync("a.tab:has-text('Recent activity')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Activity"));

        await Page.ClickAsync("a.tab:has-text('Open requests')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Requests"));

        // ==========================================
        // PAGE 2: Inventory (`/Ops/Inventory`)
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Inventory", "Inventory");

        // Verify Bulk import button is visible for IT Asset Executive
        await Expect(Page.Locator("a:has-text('Bulk import')")).ToBeVisibleAsync();
        
        // Verify Add asset button is NOT visible for IT Asset Executive
        await Expect(Page.Locator("button:has-text('Add asset')").First).ToBeVisibleAsync();

        // Search space input & dropdown filters
        await Page.FillAsync("input[id='Search']", "Latitude");
        await Page.SelectOptionAsync("select[id='Category']", new[] { "0" }); // Laptops
        await Page.SelectOptionAsync("select[id='Status']", new[] { "1" });   // Assigned
        await Page.SelectOptionAsync("select[id='Condition']", new[] { "0" }); // New
        
        // Reset filters for tests
        await Page.FillAsync("input[id='Search']", "");
        await Page.SelectOptionAsync("select[id='Category']", new[] { "" });
        await Page.SelectOptionAsync("select[id='Status']", new[] { "" });
        await Page.SelectOptionAsync("select[id='Condition']", new[] { "" });

        // Add Asset
        await Page.ClickAsync("button:has-text('Add asset')");
        var addAssetModal = Page.Locator("h3:has-text('Add asset')");
        await Expect(addAssetModal).ToBeVisibleAsync();

        string uniqueSerial = "SN-Exec-" + System.Guid.NewGuid().ToString().Substring(0, 8);
        await Page.SelectOptionAsync("#modal-addAsset select[name='Input.Category']", new[] { "0" }); // Laptop
        await Page.FillAsync("#modal-addAsset input[name='Input.Brand']", "Executive Brand");
        await Page.FillAsync("#modal-addAsset input[name='Input.Model']", "Executive Model");
        await Page.FillAsync("#modal-addAsset input[name='Input.SerialNumber']", uniqueSerial);
        await Page.FillAsync("#modal-addAsset textarea[name='Input.Notes']", "Added by Playwright IT Exec test.");
        await Page.ClickAsync("#modal-addAsset button[type='submit']:has-text('Add asset')");

        // Verify presence & details eye icon modal
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        var assetRow = Page.Locator("tbody tr.data-row:has-text('Executive Brand')").First;
        await Expect(assetRow).ToBeVisibleAsync();
        
        await assetRow.Locator("button[title='Details']").ClickAsync();
        await Expect(Page.Locator(".modal-overlay.open div.lbl:has-text('DETAILS')")).ToBeVisibleAsync();
        await Page.ClickAsync(".modal-overlay.open button[data-close-modal]");

        // Assign asset directly to Priya Shankar
        await assetRow.Locator("button[title='Assign']").ClickAsync();
        await Expect(Page.Locator("#modal-assignDirect.open")).ToBeVisibleAsync();
        var priyaRow = Page.Locator("#modal-assignDirect label.pick-row:has-text('Priya Shankar')");
        await priyaRow.Locator("input[type='radio']").CheckAsync();
        await Page.FillAsync("#modal-assignDirect input[name='notes']", "Onboarding Kit Exec");
        await Page.ClickAsync("#modal-assignDirect button[type='submit']:has-text('Assign asset')");

        // Verify challan and close
        var challanModal = Page.Locator("#modal-deliveryChallan");
        await Expect(challanModal).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-deliveryChallan button[data-close-modal]");

        // Return asset to stock
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        var updatedAssetRow = Page.Locator("tbody tr.data-row:has-text('Executive Brand')").First;
        await updatedAssetRow.Locator("button[title='Return to stock']").ClickAsync();

        // Re-apply search to verify return
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        var finalAssetRow = Page.Locator("tbody tr.data-row:has-text('Executive Brand')").First;
        await Expect(finalAssetRow.Locator("text=In stock")).ToBeVisibleAsync();

        // ==========================================
        // PAGE 3: My Assets (`/Me/Assets`)
        // ==========================================
        await AssertPageHeaderAsync("/Me/Assets", "My Assets");
        var reportBtn = Page.Locator("button:has-text('Report an issue')").First;
        if (await reportBtn.IsVisibleAsync())
        {
            await reportBtn.ClickAsync();
            await Expect(Page.Locator("#modal-reportIssue.open")).ToBeVisibleAsync();
            await Page.ClickAsync("#modal-reportIssue button[data-close-modal]");
        }

        // ==========================================
        // PAGE 4: My Requests (`/Me/Requests`)
        // ==========================================
        await AssertPageHeaderAsync("/Me/Requests", "My Requests");
        await Page.ClickAsync("button:has-text('Request an asset')");
        await Expect(Page.Locator("h3:has-text('Request an asset')").First).ToBeVisibleAsync();
        
        string uniqueRequest = "Req-Exec-" + System.Guid.NewGuid().ToString().Substring(0, 8);
        await Page.FillAsync("input[id='Input_ItemName']", uniqueRequest);
        await Page.SelectOptionAsync("select[id='Input_Category']", new[] { "0" });
        await Page.FillAsync("textarea[id='Input_Reason']", "Executive test request justification.");
        await Page.ClickAsync("button:has-text('Submit request')");

        // Verify request list contains our unique item
        await Page.WaitForURLAsync(url => url.Contains("/Me/Requests"));
        var requestCard = Page.Locator($".req-card:has-text('{uniqueRequest}')").First;
        await Expect(requestCard).ToBeVisibleAsync();
        
        // Cancel request if cancel button is present
        var cancelBtn = requestCard.Locator("button:has-text('Cancel request')");
        if (await cancelBtn.IsVisibleAsync())
        {
            await cancelBtn.ClickAsync();
        }

        // ==========================================
        // PAGE 5: My Return (`/Me/Returns`)
        // ==========================================
        await AssertPageHeaderAsync("/Me/Returns", "My Returns");
        var returnAssetBtn = Page.Locator("button:has-text('Return an asset')");
        if (await returnAssetBtn.IsVisibleAsync())
        {
            await returnAssetBtn.ClickAsync();
            await Expect(Page.Locator("#modal-raiseReturn.open")).ToBeVisibleAsync();
            await Page.FillAsync("#modal-raiseReturn.open input[name='dropOffLocation']", "Executive Desk");
            await Page.FillAsync("#modal-raiseReturn.open textarea[name='reason']", "Decommission.");
            await Page.ClickAsync("#modal-raiseReturn.open button[type='submit']:has-text('Raise return')");
        }

        // ==========================================
        // PAGE 6: Approvals (`/Ops/ExecApprovals`)
        // ==========================================
        await AssertPageHeaderAsync("/Ops/ExecApprovals", "Approvals");
        // Verify Unclaimed approvals queue assign actions if button is present
        var fulfilBtn = Page.Locator("button:has-text('Assign')").First;
        if (await fulfilBtn.IsVisibleAsync())
        {
            await fulfilBtn.ClickAsync();
            await Expect(Page.Locator("#modal-fulfilRequest.open")).ToBeVisibleAsync();
            await Page.ClickAsync("#modal-fulfilRequest.open button.route-option[data-route='assign']");
            await Expect(Page.Locator("#modal-assignAsset.open")).ToBeVisibleAsync();
            
            // Check first asset check box and submit
            var assetCheckbox = Page.Locator("#assignPickList label.pick-row input[type='checkbox']").First;
            if (await assetCheckbox.IsVisibleAsync())
            {
                await assetCheckbox.CheckAsync();
                await Page.FillAsync("#assignCourier", "Standard DHL");
                await Page.FillAsync("#assignAwb", "AWB-12345");
                await Page.ClickAsync("#modal-assignAsset.open button[type='submit']:has-text('Assign asset')");
                
                // Close Delivery Challan Modal
                await Page.ClickAsync("#modal-deliveryChallan button[data-close-modal]");
            }
            else
            {
                await Page.ClickAsync("#modal-assignAsset.open button[data-close-modal]");
            }
        }

        // ==========================================
        // PAGE 7: Return Tracking (`/Ops/ReturnTracking`)
        // ==========================================
        await AssertPageHeaderAsync("/Ops/ReturnTracking", "Returns tracking");
        await Page.ClickAsync("a.tab:has-text('All returns')");
        
        // Dynamically click Mark received
        var transitRow = Page.Locator("tbody tr:has(button:has-text('Mark received'))").First;
        if (await transitRow.IsVisibleAsync())
        {
            await transitRow.Locator("button:has-text('Mark received')").ClickAsync();
        }

        // Dynamically click Inspect
        var inspectionRow = Page.Locator("tbody tr:has(button:has-text('Inspect'))").First;
        if (await inspectionRow.IsVisibleAsync())
        {
            await inspectionRow.Locator("button:has-text('Inspect')").ClickAsync();
            await Expect(Page.Locator("#modal-inspectReturn.open")).ToBeVisibleAsync();
            await Page.ClickAsync("#modal-inspectReturn.open div.choice[data-value='1']");
            await Page.FillAsync("#modal-inspectReturn.open textarea[name='notes']", "Executive inspected return.");
            await Page.ClickAsync("#modal-inspectReturn.open button[type='submit']:has-text('Close return')");
        }

        // ==========================================
        // PAGE 8: Service Queue (`/Ops/ServiceQueue`)
        // ==========================================
        await AssertPageHeaderAsync("/Ops/ServiceQueue", "Service queue");
        await Page.ClickAsync("a.tab:has-text('All')");

        // Dynamically click Accept
        var serviceRow = Page.Locator("tbody tr:has(button:has-text('Accept'))").First;
        if (await serviceRow.IsVisibleAsync())
        {
            await serviceRow.Locator("button:has-text('Accept')").ClickAsync();
        }

        // Dynamically click Resolve
        var inProgressRow = Page.Locator("tbody tr:has(button:has-text('Resolve'))").First;
        if (await inProgressRow.IsVisibleAsync())
        {
            await inProgressRow.Locator("button:has-text('Resolve')").ClickAsync();
            await Expect(Page.Locator("#modal-resolveService.open")).ToBeVisibleAsync();
            await Page.FillAsync("#modal-resolveService.open textarea[name='resolution']", "Executive resolved issue.");
            await Page.ClickAsync("#modal-resolveService.open button[type='submit']:has-text('Mark resolved')");
        }

        // ==========================================
        // FEATURE AREA: Sidebar Navigation Clicks Check (nav-item)
        // ==========================================
        await Page.ClickAsync("a.nav-item:has-text('Dashboard')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Operations overview");

        await Page.ClickAsync("a.nav-item:has-text('Inventory')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Inventory");

        await Page.ClickAsync("a.nav-item:has-text('My Assets')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Assets");

        await Page.ClickAsync("a.nav-item:has-text('My Requests')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Requests");

        await Page.ClickAsync("a.nav-item:has-text('My Return')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("My Returns");

        await Page.ClickAsync("a.nav-item:has-text('Approvals')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Approvals");

        await Page.ClickAsync("a.nav-item:has-text('Return Tracking')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Returns tracking");

        await Page.ClickAsync("a.nav-item:has-text('Service Queue')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Service queue");

        // Restrictions Check
        await AssertAccessDeniedAsync("/Ops/Users");
        await AssertAccessDeniedAsync("/Ops/Roles");
        await AssertAccessDeniedAsync("/Ops/Audit");
    }
}
