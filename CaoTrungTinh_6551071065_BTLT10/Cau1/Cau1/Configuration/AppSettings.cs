using Microsoft.Extensions.Configuration;

namespace Cau1.Configuration;

public sealed class AppSettings
{
    public ConnectionSettings ConnectionStrings { get; set; } = new();

    public static AppSettings Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
                "Development", StringComparison.OrdinalIgnoreCase))
            builder.AddJsonFile("appsettings.Development.json", optional: true);
        var settings = builder.Build().Get<AppSettings>()
            ?? throw new InvalidOperationException("Không đọc được cấu hình ứng dụng.");
        if (string.IsNullOrWhiteSpace(settings.ConnectionStrings.ChiTieu))
            throw new InvalidOperationException("Chưa cấu hình kết nối cơ sở dữ liệu.");
        return settings;
    }
}

public sealed class ConnectionSettings
{
    public string ChiTieu { get; set; } = "";
}
