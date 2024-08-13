using WebDriverUpdateDetector.Functions;

namespace WebDriverUpdateDetector.Test;

public class ChromeBrowserDetectorTest
{
    [Test]
    public async Task GetChromeBrowserVersionsAsync_Test()
    {
        // Given
        using var testHost = TestHost.CreateHost([(
            Url: "https://dl.google.com/linux/chrome/deb/dists/stable/main/binary-amd64/Packages",
            ContentPath: "Packages"
        )]);
        var detector = testHost.Services.GetRequiredService<ChromeBrowserDetector>();

        // When
        var browserVersions = await detector.GetChromeBrowserVersionsAsync();

        // Then
        browserVersions.Is("105.0.5195.102");
    }
}
