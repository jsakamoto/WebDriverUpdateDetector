namespace WebDriverUpdateDetector;

internal class SmtpConfig
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public bool UseSSL { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
