using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Cau3.Services;

public sealed record WeatherForecast(int Code, double Minimum, double Maximum, int? Rain);
public sealed class PlaceNotFoundException : Exception;

public sealed class WeatherService
{
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly HttpClient client;
    public WeatherService(HttpClient? client = null) => this.client = client ?? SharedClient;

    public async Task<WeatherForecast?> GetAsync(string place, DateOnly date, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        // forecast_days=16 contains today and the next 15 dates.
        if (date < today || date > today.AddDays(15)) return null;
        var location = await FindLocationAsync(place, cancellationToken);
        if (location is null)
        {
            string fallback = RemoveAccents(place);
            if (fallback != place) location = await FindLocationAsync(fallback, cancellationToken);
        }
        if (location is null) throw new PlaceNotFoundException();
        string url = FormattableString.Invariant($"https://api.open-meteo.com/v1/forecast?latitude={location.Value.Latitude}&longitude={location.Value.Longitude}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto&forecast_days=16");
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return ParseDaily(json.RootElement, date);
    }

    private async Task<(double Latitude, double Longitude)?> FindLocationAsync(string name, CancellationToken token)
    {
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(name.Trim())}&count=1&language=vi";
        using var response = await client.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
        if (!json.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return null;
        return (results[0].GetProperty("latitude").GetDouble(), results[0].GetProperty("longitude").GetDouble());
    }

    public static WeatherForecast? ParseDaily(JsonElement root, DateOnly date)
    {
        if (!root.TryGetProperty("daily", out var daily) || !daily.TryGetProperty("time", out var dates)) return null;
        string target = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        int index = -1;
        for (int i = 0; i < dates.GetArrayLength(); i++) if (dates[i].GetString() == target) { index = i; break; }
        if (index < 0) return null;
        double? Read(string key)
        {
            if (!daily.TryGetProperty(key, out var values) || index >= values.GetArrayLength() || values[index].ValueKind != JsonValueKind.Number) return null;
            return values[index].GetDouble();
        }
        double? code = Read("weather_code"), min = Read("temperature_2m_min"), max = Read("temperature_2m_max"), rain = Read("precipitation_probability_max");
        if (code is null || min is null || max is null) return null;
        return new((int)code.Value, min.Value, max.Value, rain is null ? null : (int)rain.Value);
    }

    public static string RemoveAccents(string text)
    {
        string normalized = text.Normalize(NormalizationForm.FormD);
        return new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
            .Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormC);
    }

    public static string Kind(int code) => code switch
    {
        0 => "clear", >= 1 and <= 3 => "cloud", 45 or 48 => "fog",
        >= 51 and <= 67 or >= 80 and <= 82 => "rain",
        >= 71 and <= 77 or 85 or 86 => "snow", >= 95 and <= 99 => "storm", _ => "unknown"
    };
}
