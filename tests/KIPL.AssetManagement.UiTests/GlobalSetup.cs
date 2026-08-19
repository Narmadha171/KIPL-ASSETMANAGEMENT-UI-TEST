using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace KIPL.AssetManagement.UiTests;

[SetUpFixture]
public class GlobalSetup
{
    private Process? _webAppProcess;

    [OneTimeSetUp]
    public async Task StartWebApp()
    {
        // Force Playwright to run in headed mode locally, but keep headless for CI/CD (GitHub Actions)
        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true")
        {
            Environment.SetEnvironmentVariable("HEADED", "1");
        }

        var srcDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\..\..\src\KIPL.AssetManagement.Web"));
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --launch-profile https",
            WorkingDirectory = srcDir,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false
        };
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";

        _webAppProcess = Process.Start(startInfo);
        
        // Wait for the app to be responsive
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        using var client = new HttpClient(handler);
        
        int retries = 30;
        while (retries > 0)
        {
            try
            {
                var response = await client.GetAsync("https://localhost:7218");
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Found)
                {
                    break;
                }
            }
            catch
            {
                // Ignore connection errors and keep waiting
            }
            
            await Task.Delay(1000);
            retries--;
        }
        
        if (retries == 0)
        {
            throw new Exception("Web application failed to start within the timeout period.");
        }
    }

    [OneTimeTearDown]
    public void StopWebApp()
    {
        if (_webAppProcess != null && !_webAppProcess.HasExited)
        {
            _webAppProcess.Kill(true); // Kill process tree
            _webAppProcess.Dispose();
        }
    }
}
