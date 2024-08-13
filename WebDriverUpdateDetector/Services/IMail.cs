namespace WebDriverUpdateDetector.Services;

public interface IMail
{
    ValueTask SendAsync(string subject, string body);
}
