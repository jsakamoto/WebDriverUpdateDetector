using WebDriverUpdateDetector.Functions;

namespace WebDriverUpdateDetector.Test;

internal class ChromeDriverDetectorTest
{
    [TestCase("last-known-good-versions-A.json", new[] { "115.0.5790.98", "116.0.5845.32" })]
    [TestCase("last-known-good-versions-B.json", new[] { "115.0.5790.98" })]
    public async Task GetChromeDriverVersionsAsync_Test(string versionIfoFileName, string[] expectedVersions)
    {
        // Given
        using var testHost = TestHost.CreateHost([(
            Url: "https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json",
            ContentPath: versionIfoFileName
        )]);
        var detector = testHost.Services.GetRequiredService<ChromeDriverDetector>();

        // When
        var versions = await detector.GetChromeDriverVersionsAsync();

        // Then: It returns the stable and beta versions of ChromeDriver.
        versions.Is(expectedVersions);
    }
}
