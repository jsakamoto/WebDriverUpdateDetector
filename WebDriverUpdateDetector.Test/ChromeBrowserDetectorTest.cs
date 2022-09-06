using Toolbelt;

namespace WebDriverUpdateDetector.Test;

public class ChromeBrowserDetectorTest
{
    [Test]
    public async Task GetChromeBrowserVersionsAsync_Test()
    {
        // Given
        var projectDir = FileIO.FindContainerDirToAncestor("*.csproj");
        using var packageStream = File.OpenRead(Path.Combine(projectDir, "Fixtures", "Packages"));

        // When
        var browserVersions = await ChromeBrowserDetector.GetChromeBrowserVersionsAsync(packageStream);

        // Then
        browserVersions.Is("105.0.5195.102");
    }
}
