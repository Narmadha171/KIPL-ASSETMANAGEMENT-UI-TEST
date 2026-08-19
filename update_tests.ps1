# Insert checks into AdminTests.cs
$content = Get-Content 'tests\KIPL.AssetManagement.UiTests\AdminTests.cs' -Raw
$content = $content -replace '(?m)^(\s*)(// Add Asset Modal & Creation)', "$1// Assert Export button visibility
$1await Expect(Page.Locator("a:has-text('Export')")).ToBeVisibleAsync();
$1$2"
Set-Content -Path 'tests\KIPL.AssetManagement.UiTests\AdminTests.cs' -Value $content

# Insert checks into EmployeeTests.cs
$content = Get-Content 'tests\KIPL.AssetManagement.UiTests\EmployeeTests.cs' -Raw
$content = $content -replace '(?m)^(\s*)(// Verify Sidebar Navigation Links for Employee)', "$1// Verify Dashboard Buttons
$1await Page.GotoAsync(BaseUrl + "/Me/Dashboard");
$1await Expect(Page.Locator("a.btn:has-text('Return an asset')").First).ToBeVisibleAsync();
$1await Expect(Page.Locator("a.btn:has-text('Request an asset')").First).ToBeVisibleAsync();

$1$2"
$content = $content -replace '(?m)^(\s*)(// Verify the requested item appears in the list after refresh)', "$1$2
$1await Page.WaitForURLAsync(url => url.Contains("/Me/Requests"));
$1var cancelBtn = Page.Locator("$("text=$uniqueRequestItem")").Locator("..").Locator("..").Locator("button:has-text('Cancel request')");
$1await Expect(cancelBtn).ToBeVisibleAsync();
"
Set-Content -Path 'tests\KIPL.AssetManagement.UiTests\EmployeeTests.cs' -Value $content
