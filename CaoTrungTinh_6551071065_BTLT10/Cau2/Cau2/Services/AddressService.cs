using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Cau2.Services;

public sealed class AddressItem
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("wards")] public List<AddressItem> Wards { get; set; } = [];
}

public static class AddressService
{
    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri("https://provinces.open-api.vn/api/v2/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    public static async Task<List<AddressItem>> GetProvincesAsync(CancellationToken token) =>
        await Client.GetFromJsonAsync<List<AddressItem>>("", token) ?? throw new HttpRequestException("Danh sách tỉnh trống.");

    public static async Task<List<AddressItem>> GetWardsAsync(int code, CancellationToken token) =>
        (await Client.GetFromJsonAsync<AddressItem>($"p/{code}?depth=2", token)
        ?? throw new HttpRequestException("Không có dữ liệu phường/xã.")).Wards;
}
