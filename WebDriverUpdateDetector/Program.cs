using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebDriverUpdateDetector.Internal;
using WebDriverUpdateDetector.Internal.Mail;
using WebDriverUpdateDetector.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(config =>
    {
        config.AddUserSecrets<Program>();
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddOptions<SmtpConfig>().BindConfiguration("Smtp");
        services.AddOptions<NotifyMailConfig>().BindConfiguration("NotifyMail");
        services.AddTransient<IMail, Mail>();
        services.AddTransient<IAzureTableStorage, AzureTableStorage>();
        services.AddTransient<HttpClient>();
    })
    .Build();

host.Run();
