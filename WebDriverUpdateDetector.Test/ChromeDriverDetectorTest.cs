using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WebDriverUpdateDetector.Test.Fixtures;

namespace WebDriverUpdateDetector.Test;

internal class ChromeDriverDetectorTest
{
    [TestCase("last-known-good-versions-A.json", new[] { "115.0.5790.98", "116.0.5845.32" })]
    [TestCase("last-known-good-versions-B.json", new[] { "115.0.5790.98" })]
    public async Task GetChromeDriverVersionsAsync_Test(string versionIfoFileName, string[] expectedVersions)
    {
        // Given
        var baseUrl = $"http://localhost:{TestHelper.GetAvailableIPv4Port()}";
        await using var app = WebApplication.CreateBuilder().Build();
        app.Map("/last-known-good-versions.json", () => Results.Content(File.ReadAllText(TestHelper.GetFixturePath(versionIfoFileName)), contentType: "application/json"));
        app.Urls.Add(baseUrl);
        await app.StartAsync();

        // When
        var chromeDiverVersionUrl = baseUrl + "/last-known-good-versions.json";
        var versions = await ChromeDriverDetector.GetChromeDriverVersionsAsync(chromeDiverVersionUrl);

        // Then: It returns the stable and beta versions of ChromeDriver.
        versions.Is(expectedVersions);
    }
}
