using Microsoft.Extensions.Configuration;

namespace WebDriverUpdateDetector.Internal;

internal static class Configuration
{
    public static T GetSection<T>(this IConfiguration configuration, string key) where T : new()
    {
        var obj = new T();
        configuration.GetSection(key).Bind(obj);
        return obj;
    }
}
