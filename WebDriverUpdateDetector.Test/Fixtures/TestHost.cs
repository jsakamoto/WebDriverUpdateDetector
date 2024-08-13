using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WebDriverUpdateDetector.Test.Fixtures;

internal class TestHost
{
    private class HttpMockMessageHandler(IEnumerable<(string? Url, string ContentPath)> responseList) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = responseList.FirstOrDefault(x => x.Url == request.RequestUri?.AbsoluteUri);
            var message = new HttpResponseMessage(response.Url is not null ? HttpStatusCode.OK : HttpStatusCode.NotFound);
            if (!string.IsNullOrEmpty(response.ContentPath)) message.Content = new StringContent(File.ReadAllText(TestHelper.GetFixturePath(response.ContentPath)));
            return Task.FromResult(message);
        }
    }

    public static IHost CreateHost(IEnumerable<(string? Url, string ContentPath)> responseList) => new HostBuilder()
        .ConfigureServices(services =>
        {
            services.AddTransient(_ => new HttpClient(new HttpMockMessageHandler(responseList)));
            services.AddOptions<SmtpConfig>().BindConfiguration("Smtp");
            services.AddOptions<NotifyMailConfig>().BindConfiguration("NotifyMail");
            services.AddTransient<Mail>();
            services.AddTransient<AzureTableStorage>();
            services.AddTransient<ILoggerFactory, NullLoggerFactory>();
            services.AddTransient<ChromeBrowserDetector>();
            services.AddTransient<ChromeDriverDetector>();
            services.AddTransient<IEDriverDetector>();
        })
        .Build();
}
