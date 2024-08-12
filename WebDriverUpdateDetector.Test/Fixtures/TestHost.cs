using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WebDriverUpdateDetector.Test.Fixtures;

internal class TestHost
{
    public static IHost CreateHost()
    {
        var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddOptions<SmtpConfig>().BindConfiguration("Smtp");
                services.AddOptions<NotifyMailConfig>().BindConfiguration("NotifyMail");
                services.AddTransient<Mail>();
                services.AddTransient<AzureTableStorage>();
                services.AddTransient<HttpClient>();
                services.AddTransient<HttpClient>();
                services.AddTransient<ILoggerFactory, NullLoggerFactory>();
                services.AddTransient<ChromeBrowserDetector>();
                services.AddTransient<ChromeDriverDetector>();
                services.AddTransient<IEDriverDetector>();
            })
            .Build();

        return host;
    }
}
