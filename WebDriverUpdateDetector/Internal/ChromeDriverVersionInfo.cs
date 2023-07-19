namespace WebDriverUpdateDetector.Internal;

internal class ChromeDriverVersionInfo
{
    public class ChannelInfo
    {
        public string Channel { get; set; } = "";
        public string Version { get; set; } = "";
        public string Revision { get; set; } = "";
    }

    public class ChannelInfoList
    {
        public ChannelInfo Stable { get; set; } = new();
        public ChannelInfo Beta { get; set; } = new();
        public ChannelInfo Dev { get; set; } = new();
        public ChannelInfo Canary { get; set; } = new();
    }

    public DateTime Timestamp { get; set; }

    public ChannelInfoList Channels { get; set; } = new();
}
