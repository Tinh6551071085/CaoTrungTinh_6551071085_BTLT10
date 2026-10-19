using System.Text.Json;

namespace Cau3.Services;

public sealed class UserSettings
{
    public string Language { get; set; } = "vi-VN";
}

public static class AppSettings
{
    public static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QL_DaNgoai", "usersettings.json");

    public static UserSettings LoadLanguage()
    {
        try
        {
            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public static void SaveLanguage(string language)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        string temporary = SettingsPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new UserSettings { Language = language }));
        File.Move(temporary, SettingsPath, true);
    }

    public static string ConnectionString()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
        return json.RootElement.GetProperty("ConnectionStrings").GetProperty("QL_DaNgoai").GetString()
            ?? throw new InvalidDataException("Missing QL_DaNgoai connection string.");
    }
}
