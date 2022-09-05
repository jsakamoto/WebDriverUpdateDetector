namespace WebDriverUpdateDetector.Test;

public class IEDriverDetectorTest
{
    [Test]
    public async Task GetIEDriverVersionns_Test()
    {
        const string ieDriverChangeLogUrl = "https://raw.githubusercontent.com/SeleniumHQ/selenium/ef15a22dc01a6c02135163d065898e233c5979e5/cpp/IEDriverServer/CHANGELOG";
        var driverVersions = await IEDriverDetector.GetIEDriverVersionsAsync(ieDriverChangeLogUrl);
        driverVersions.Is(
            "2.26.1.0",
            "2.26.0.9",
            "2.26.0.8",
            "2.26.0.7",
            "2.26.0.6",
            "2.26.0.5",
            "2.26.0.4",
            "2.26.0.3",
            "2.26.0.2",
            "2.26.0.1",
            "2.26.0.0",
            "2.25.3.6",
            "2.25.3.5",
            "2.25.3.4",
            "2.25.3.3",
            "2.25.3.2",
            "2.25.3.1",
            "2.25.3.0",
            "2.25.2.3",
            "2.25.2.2",
            "2.25.2.1",
            "2.25.2.0",
            "2.25.1.0",
            "2.25.0.0",
            "2.24.2.0",
            "2.24.1.0",
            "2.24.0.0",
            "2.23.2.0",
            "2.23.1.1",
            "2.23.1.0",
            "2.23.0.0",
            "2.22.1.1",
            "2.22.1.0",
            "2.22.0.0");
    }
}