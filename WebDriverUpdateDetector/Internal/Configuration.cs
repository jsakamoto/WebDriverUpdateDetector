using Microsoft.Extensions.Configuration;

namespace WebDriverUpdateDetector;

internal static class Configuration
{
    public static IConfigurationRoot GetConfiguration()
    {
        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets(userSecretsId: "815b4b57-4eaa-4e43-9b62-7667c1949b86")
            .Build();
    }

    public static T GetSection<T>(this IConfiguration configuration, string key) where T : new()
    {
        var obj = new T();
        configuration.GetSection(key).Bind(obj);
        return obj;
    }
}
