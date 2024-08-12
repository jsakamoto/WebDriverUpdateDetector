using Microsoft.Extensions.DependencyInjection;
using Toolbelt;
using WebDriverUpdateDetector.Test.Fixtures;

namespace WebDriverUpdateDetector.Test;

public class ChromeBrowserDetectorTest
{
    [Test]
    public async Task GetChromeBrowserVersionsAsync_Test()
    {
        // Given
        using var testHost = TestHost.CreateHost();
        var detector = testHost.Services.GetRequiredService<ChromeBrowserDetector>();

        var projectDir = FileIO.FindContainerDirToAncestor("*.csproj");
        using var packageStream = File.OpenRead(Path.Combine(projectDir, "Fixtures", "Packages"));

        // When
        var browserVersions = await detector.GetChromeBrowserVersionsAsync(packageStream);

        // Then
        browserVersions.Is("105.0.5195.102");
    }
}
