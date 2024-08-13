using Toolbelt;

namespace WebDriverUpdateDetector.Test.Fixtures;

internal class TestHelper
{
    internal static string GetFixturePath(string fileName)
    {
        var projectDir = FileIO.FindContainerDirToAncestor("*.csproj");
        return Path.Combine(projectDir, "Fixtures", fileName);
    }
}
