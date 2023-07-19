using System.Net;
using System.Net.Sockets;
using Toolbelt;

namespace WebDriverUpdateDetector.Test.Fixtures;

internal class TestHelper
{
    internal static string GetFixturePath(string fileName)
    {
        var projectDir = FileIO.FindContainerDirToAncestor("*.csproj");
        return Path.Combine(projectDir, "Fixtures", fileName);
    }

    internal static int GetAvailableIPv4Port()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
