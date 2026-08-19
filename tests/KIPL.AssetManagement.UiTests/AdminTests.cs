using Microsoft.Playwright;
using NUnit.Framework;

namespace KIPL.AssetManagement.UiTests;

[TestFixture]
public class AdminTests : BasePageTest
{
    [Test]
    public async Task Admin_Verify_All_Features_In_Single_Session()
    {
        // 1. Log in once as Admin
        await LoginAsAsync("admin@kipl.com");

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
        // FEATURE AREA: Dashboard KPI Cards Navigation ("Touch the Boxes")
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Dashboard", "Operations overview");

        // A. PENDING APPROVAL box
        await Page.ClickAsync("a.kpi-card:has-text('PENDING APPROVAL')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/HrApprovals"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // B. UNCLAIMED box
        await Page.ClickAsync("a.kpi-card:has-text('UNCLAIMED')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/ExecApprovals"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // C. IN FULFILMENT box
        await Page.ClickAsync("a.kpi-card:has-text('IN FULFILMENT')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/ExecApprovals"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // D. IN STOCK box
        await Page.ClickAsync("a.kpi-card:has-text('IN STOCK')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // E. SERVICE box
        await Page.ClickAsync("a.kpi-card:has-text('SERVICE')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/ServiceQueue"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // F. LOST box
        await Page.ClickAsync("a.kpi-card:has-text('LOST')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory.*"));
        await Page.GotoAsync(BaseUrl + "/Ops/Dashboard");

        // ==========================================
        // FEATURE AREA: Dashboard Sub-tabs
        // ==========================================
        // Click and check "Returns" tab
        await Page.ClickAsync("a.tab:has-text('Returns')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Returns"));

        // Click and check "Service queue" tab
        await Page.ClickAsync("a.tab:has-text('Service queue')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Service"));

        // Click and check "Recent activity" tab
        await Page.ClickAsync("a.tab:has-text('Recent activity')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Activity"));

        // Click and check "Open requests" tab
        await Page.ClickAsync("a.tab:has-text('Open requests')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*Tab=Requests"));

        // ==========================================
        // FEATURE AREA: Inventory Search, Filters & Add Asset
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Inventory", "Inventory");

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

        // Bulk Import
        await Page.ClickAsync("a:has-text('Bulk import')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/BulkImport"));
        await Page.GotoAsync(BaseUrl + "/Ops/Inventory");

        // Add Asset Modal & Creation
        await Page.ClickAsync("button:has-text('Add asset')");
        await Expect(Page.Locator("#modal-addAsset.open")).ToBeVisibleAsync();

        string uniqueSerial = "SN-Playwright-" + System.Guid.NewGuid().ToString().Substring(0, 8);
        await Page.SelectOptionAsync("#modal-addAsset select[name='Input.Category']", new[] { "0" }); // 0 matches Laptop
        await Page.FillAsync("#modal-addAsset input[name='Input.Brand']", "Playwright Brand");
        await Page.FillAsync("#modal-addAsset input[name='Input.Model']", "Super Model");
        await Page.FillAsync("#modal-addAsset input[name='Input.SerialNumber']", uniqueSerial);
        await Page.FillAsync("#modal-addAsset textarea[name='Input.Notes']", "Added by Playwright automated test execution.");
        await Page.ClickAsync("#modal-addAsset button[type='submit']:has-text('Add asset')");

        // Verify redirect and presence of the new asset
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/Ops/Inventory"));
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        await Expect(Page.Locator("text=Playwright Brand").First).ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Asset Assignment & Return (Unassign) Workflows
        // ==========================================
        var assetRow = Page.Locator("tbody tr.data-row:has-text('Playwright Brand')").First;
        
        // Click Assign direct on the Playwright Brand row
        await assetRow.Locator("button[title='Assign']").ClickAsync();
        await Expect(Page.Locator("#modal-assignDirect.open")).ToBeVisibleAsync();

        // Select employee Priya Shankar
        var priyaRow = Page.Locator("#modal-assignDirect label.pick-row:has-text('Priya Shankar')");
        await priyaRow.Locator("input[type='radio']").CheckAsync();
        
        // Fill out notes and submit
        await Page.FillAsync("#modal-assignDirect input[name='notes']", "Onboarding Kit Direct");
        await Page.ClickAsync("#modal-assignDirect button[type='submit']:has-text('Assign asset')");

        // Verify Challan details open and dismiss the modal
        var challanModal = Page.Locator("#modal-deliveryChallan");
        await Expect(challanModal).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-deliveryChallan button[data-close-modal]");

        // Search for the serial number again to make sure the row is filtered and visible
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");

        // Return (Unassign) the asset back to stock to ensure tests are idempotent (pristine database)
        var updatedAssetRow = Page.Locator("tbody tr.data-row:has-text('Playwright Brand')").First;
        await updatedAssetRow.Locator("button[title='Return to stock']").ClickAsync();

        // Re-apply search to verify return
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        var finalAssetRow = Page.Locator("tbody tr.data-row:has-text('Playwright Brand')").First;
        await Expect(finalAssetRow.Locator("text=In stock")).ToBeVisibleAsync();

        // 1. Verify Details button and modal
        await finalAssetRow.Locator("button[title='Details']").ClickAsync();
        await Expect(Page.Locator(".modal-overlay.open div.lbl:has-text('DETAILS')")).ToBeVisibleAsync();
        await Page.ClickAsync(".modal-overlay.open button[data-close-modal]");

        // 2. Verify Edit Asset button and modal
        await finalAssetRow.Locator("button[title='Edit']").ClickAsync();
        await Expect(Page.Locator(".modal-overlay.open h3:has-text('Edit asset')")).ToBeVisibleAsync();
        await Page.FillAsync(".modal-overlay.open textarea[name='Input.Notes']", "Playwright verified notes");
        await Page.ClickAsync(".modal-overlay.open button[type='submit']:has-text('Save changes')");

        // Verify notes updated via Edit Modal
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        finalAssetRow = Page.Locator("tbody tr.data-row:has-text('Playwright Brand')").First;
        await finalAssetRow.Locator("button[title='Edit']").ClickAsync();
        await Expect(Page.Locator(".modal-overlay.open textarea[name='Input.Notes']")).ToHaveValueAsync("Playwright verified notes");
        await Page.ClickAsync(".modal-overlay.open button[data-close-modal]");

        // 3. Verify Print Label button and modal
        await finalAssetRow.Locator("button[title='Print label']").ClickAsync();
        await Expect(Page.Locator("#modal-printLabel.open")).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-printLabel.open button[data-close-modal]");

        // 4. Verify Send to Service button and status
        await finalAssetRow.Locator("button[title='Retire / lost']").ClickAsync();
        await Expect(Page.Locator("#modal-retireOrLost.open")).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-retireOrLost.open button[type='submit']:has-text('Send to service')");

        // Re-apply search to verify "Under service" status
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        var serviceAssetRow = Page.Locator("tbody tr.data-row:has-text('Playwright Brand')").First;
        await Expect(serviceAssetRow.Locator("span:has-text('Under service')")).ToBeVisibleAsync();

        // 5. Verify Retire Asset button and status
        await serviceAssetRow.Locator("button[title='Retire / lost']").ClickAsync();
        await Expect(Page.Locator("#modal-retireOrLost.open")).ToBeVisibleAsync();
        await Page.FillAsync("#modal-retireOrLost.open input[name='reason']", "Decommissioned via E2E test");
        await Page.ClickAsync("#modal-retireOrLost.open button[type='submit']:has-text('Retire asset')");

        // Re-apply search to verify "Retired" status
        await Page.FillAsync("input[id='Search']", uniqueSerial);
        await Page.PressAsync("input[id='Search']", "Enter");
        var retiredAssetRow = Page.Locator("tbody tr.data-row:has-text('Playwright Brand')").First;
        await Expect(retiredAssetRow.Locator("span:has-text('Retired')")).ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: User Edit Workflow & Filters
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Users", "User management");

        // Search space input & dropdown filters on Users Page
        await Page.FillAsync("input[id='Search']", "Priya");
        await Page.SelectOptionAsync("select[id='Role']", new[] { "Employee" });
        await Page.ClickAsync("button:has-text('Apply')");
        await Page.ClickAsync("a:has-text('Reset')");

        // Verify Add User button modal
        await Page.ClickAsync("button[data-modal='addUser']");
        await Expect(Page.Locator("#modal-addUser.open")).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-addUser.open button[data-close-modal]");

        // Deactivate User (Resign action)
        await Page.FillAsync("input[id='Search']", "John Doe");
        await Page.ClickAsync("button:has-text('Apply')");
        var johnRow = Page.Locator("tbody#userRows tr:has-text('John Doe')").First;
        await johnRow.Locator("button:has-text('Deactivate')").ClickAsync();
        await Expect(johnRow.Locator("span:has-text('Inactive')")).ToBeVisibleAsync();

        // Restore to Active (Idempotence)
        await johnRow.Locator("button:has-text('Edit')").ClickAsync();
        await Expect(Page.Locator("div.modal-overlay.open")).ToBeVisibleAsync();
        await Page.SelectOptionAsync("div.modal-overlay.open select[name='EditInput.Status']", new[] { "0" }); // 0 matches Active
        await Page.ClickAsync("div.modal-overlay.open button[type='submit']:has-text('Save changes')");
        await Expect(johnRow.Locator("span:has-text('Active')")).ToBeVisibleAsync();
        
        // Search and find Vikram Singh row
        await Page.FillAsync("input[id='Search']", "Vikram");
        await Page.ClickAsync("button:has-text('Apply')");
        var userRow = Page.Locator("tbody#userRows tr:has-text('Vikram Singh')").First;

        // Click Edit to open user edit modal
        await userRow.Locator("button:has-text('Edit')").ClickAsync();
        await Expect(Page.Locator("div.modal-overlay.open")).ToBeVisibleAsync();
        var editModal = Page.Locator("div.modal-overlay.open");

        // Change Name and save
        await editModal.Locator("input[name='EditInput.FullName']").FillAsync("Vikram Singh Edited");
        await editModal.Locator("button[type='submit']:has-text('Save changes')").ClickAsync();

        // Verify list display updates
        await Expect(Page.Locator("tbody#userRows tr:has-text('Vikram Singh Edited')").First).ToBeVisibleAsync();

        // Edit back to original name for teardown
        await Page.Locator("tbody#userRows tr:has-text('Vikram Singh Edited')").First.Locator("button:has-text('Edit')").ClickAsync();
        await Expect(Page.Locator("div.modal-overlay.open")).ToBeVisibleAsync();
        await editModal.Locator("input[name='EditInput.FullName']").FillAsync("Vikram Singh");
        await editModal.Locator("button[type='submit']:has-text('Save changes')").ClickAsync();
        await Expect(Page.Locator("tbody#userRows tr:has-text('Vikram Singh')").First).ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Roles Page Verification
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Roles", "Roles and permissions");

        // Clicks "Create role" button and close
        await Page.ClickAsync("button:has-text('Create role')");
        await Expect(Page.Locator("#modal-createRole.open")).ToBeVisibleAsync();
        await Page.ClickAsync("#modal-createRole.open button[data-close-modal]");

        // Click through role tabs
        await Page.ClickAsync("a.tab:has-text('Admin')");
        await Page.ClickAsync("a.tab:has-text('IT Asset Manager')");
        await Page.ClickAsync("a.tab:has-text('IT Asset Executive')");

        // ==========================================
        // FEATURE AREA: Service Queue & Return Tracking & Bulk Import
        // ==========================================
        // 1. Bulk Import template download
        await AssertPageHeaderAsync("/Ops/BulkImport", "Bulk import");
        await Expect(Page.Locator("a:has-text('Download template')")).ToBeVisibleAsync();
        await Page.ClickAsync("a:has-text('Download template')");

        // 2. Service Queue Accept and Resolve
        await AssertPageHeaderAsync("/Ops/ServiceQueue", "Service queue");
        var serviceRow = Page.Locator("tbody tr:has(button:has-text('Accept'))").First;
        if (await serviceRow.IsVisibleAsync())
        {
            await serviceRow.Locator("button:has-text('Accept')").ClickAsync();
        }
        var inProgressRow = Page.Locator("tbody tr:has(button:has-text('Resolve'))").First;
        if (await inProgressRow.IsVisibleAsync())
        {
            await inProgressRow.Locator("button:has-text('Resolve')").ClickAsync();
            await Expect(Page.Locator("#modal-resolveService.open")).ToBeVisibleAsync();
            await Page.FillAsync("#modal-resolveService.open textarea[name='resolution']", "Battery replaced under warranty.");
            await Page.ClickAsync("#modal-resolveService.open button[type='submit']:has-text('Mark resolved')");
        }

        // 3. Return Tracking Mark Received and Inspect
        await AssertPageHeaderAsync("/Ops/ReturnTracking", "Returns tracking");
        var transitRow = Page.Locator("tbody tr:has(button:has-text('Mark received'))").First;
        if (await transitRow.IsVisibleAsync())
        {
            await transitRow.Locator("button:has-text('Mark received')").ClickAsync();
        }
        
        var inspectionRow = Page.Locator("tbody tr:has(button:has-text('Inspect'))").First;
        if (await inspectionRow.IsVisibleAsync())
        {
            await inspectionRow.Locator("button:has-text('Inspect')").ClickAsync();
            await Expect(Page.Locator("#modal-inspectReturn.open")).ToBeVisibleAsync();
            await Page.ClickAsync("#modal-inspectReturn.open div.choice[data-value='1']"); // Good condition
            await Page.FillAsync("#modal-inspectReturn.open textarea[name='notes']", "Checked and functional.");
            await Page.ClickAsync("#modal-inspectReturn.open button[type='submit']:has-text('Close return')");
        }

        // ==========================================
        // FEATURE AREA: Audit Trail Verification & Filters
        // ==========================================
        await AssertPageHeaderAsync("/Ops/Audit", "Audit trail");

        // Audit Trail filter inputs & dropdowns
        await Page.FillAsync("input[id='Search']", "System Admin");
        var actionOptions = await Page.Locator("select[id='ActionName'] option").AllAsync();
        if (actionOptions.Count > 1)
        {
            var val = await actionOptions[1].GetAttributeAsync("value");
            if (!string.IsNullOrEmpty(val))
            {
                await Page.SelectOptionAsync("select[id='ActionName']", new[] { val });
            }
        }
        await Page.SelectOptionAsync("select[id='RoleName']", new[] { "Admin" });
        await Page.ClickAsync("button:has-text('Apply')");
        await Page.ClickAsync("a:has-text('Reset')");

        // Verify administrator Full Name log present in recent entries
        await Expect(Page.Locator("tbody tr").First.Locator("text=System Admin")).ToBeVisibleAsync();

        // ==========================================
        // FEATURE AREA: Sidebar Sidebar Navigation Links Check
        // ==========================================
        await Page.ClickAsync("a.nav-item:has-text('Dashboard')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Operations overview");

        await Page.ClickAsync("a.nav-item:has-text('Inventory')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Inventory");

        await Page.ClickAsync("a.nav-item:has-text('Roles & Permissions')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Roles and permissions");

        await Page.ClickAsync("a.nav-item:has-text('Users')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("User management");

        await Page.ClickAsync("a.nav-item:has-text('Audit Trail')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Audit trail");

        await Page.ClickAsync("a.nav-item:has-text('Approvals')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Approvals");

        await Page.ClickAsync("a.nav-item:has-text('Return Tracking')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Returns tracking");

        await Page.ClickAsync("a.nav-item:has-text('Service Queue')");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Service queue");
    }
    [Test]
    public async Task Admin_Verify_Role_Permission_Toggle()
    {
        // 1. Log in as Admin
        await LoginAsAsync("admin@kipl.com");

        // 2. Go to Roles page and click IT Asset Manager tab
        await Page.GotoAsync($"{BaseUrl}/Ops/Roles");
        await Page.ClickAsync("a.tab:has-text('IT Asset Manager')");

        var toggleBtnLocator = Page.Locator("form:has(input[name='permission'][value='audit.view']) button.perm-switch");

        // Turn OFF the permission
        await toggleBtnLocator.ClickAsync();
        
        // Wait for page to reload and verify toggle button no longer has the 'on' class
        await Expect(toggleBtnLocator).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(".*on.*"));

        try
        {
            // 3. Verify Access is Denied for IT Asset Manager
            await Page.Context.ClearCookiesAsync(); // Log out
            await LoginAsAsync("vikram.singh@kipl.com"); // Log in as IT Asset Manager
            
            // Should redirect to AccessDenied when trying to view audit trail
            await AssertAccessDeniedAsync("/Ops/Audit");

            // 4. Restore Access via Admin
            await Page.Context.ClearCookiesAsync(); // Log out
            await LoginAsAsync("admin@kipl.com");
            
            await Page.GotoAsync($"{BaseUrl}/Ops/Roles");
            await Page.ClickAsync("a.tab:has-text('IT Asset Manager')");
            
            // Turn ON the permission
            await toggleBtnLocator.ClickAsync();
            await Expect(toggleBtnLocator).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(".*on.*"));

            // 5. Verify Access is Restored for IT Asset Manager
            await Page.Context.ClearCookiesAsync(); // Log out
            await LoginAsAsync("vikram.singh@kipl.com");
            
            // Should be allowed to view audit page header
            await AssertPageHeaderAsync("/Ops/Audit", "Audit trail");
        }
        finally
        {
            // 6. Clean up: Ensure it's left ON to avoid breaking other tests
            await Page.Context.ClearCookiesAsync();
            await LoginAsAsync("admin@kipl.com");
            await Page.GotoAsync($"{BaseUrl}/Ops/Roles");
            await Page.ClickAsync("a.tab:has-text('IT Asset Manager')");
            
            var classAttr = await toggleBtnLocator.GetAttributeAsync("class");
            if (classAttr != null && !classAttr.Contains("on"))
            {
                await toggleBtnLocator.ClickAsync();
            }
        }
    }
}
