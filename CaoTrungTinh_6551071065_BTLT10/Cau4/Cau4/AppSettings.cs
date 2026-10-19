using System.Text.Json;

namespace Cau4;

public sealed class AppSettings
{
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
    public PaymentSettings Payment { get; set; } = new();

    public static AppSettings Load()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Không đọc được appsettings.json.");
    }

    public string DatabaseConnection => ConnectionStrings.TryGetValue("TraSua", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value : throw new InvalidOperationException("Thiếu ConnectionStrings:TraSua trong appsettings.json.");
}

public sealed class PaymentSettings
{
    public string BankId { get; set; } = "";
    public string AccountNo { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Template { get; set; } = "";
    public string ShopName { get; set; } = "";

    public void Validate()
    {
        if (new[] { BankId, AccountNo, AccountName, Template, ShopName }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Điền đủ BankId, AccountNo, AccountName, Template và ShopName trong mục Payment của appsettings.json.");
        if (!BankId.All(char.IsAsciiLetterOrDigit) || !AccountNo.All(char.IsAsciiDigit) || !Template.All(char.IsAsciiLetterOrDigit))
            throw new InvalidOperationException("BankId/Template chỉ gồm chữ và số; AccountNo chỉ gồm chữ số.");
    }
}
