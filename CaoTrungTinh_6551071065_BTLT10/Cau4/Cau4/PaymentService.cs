using System.Globalization;
using Serilog;

namespace Cau4;

internal static class PaymentService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(15) };
    public static string TransferContent(int invoiceId) => $"TRASUA HD{invoiceId}";

    public static string BuildUrl(PaymentSettings settings, int invoiceId, decimal amount)
    {
        settings.Validate();
        if (invoiceId <= 0 || amount <= 0 || decimal.Truncate(amount) != amount) throw new ArgumentException("Mã hóa đơn và số tiền không hợp lệ.");
        return $"https://img.vietqr.io/image/{Uri.EscapeDataString(settings.BankId)}-{Uri.EscapeDataString(settings.AccountNo)}-{Uri.EscapeDataString(settings.Template)}.png"
            + $"?amount={Uri.EscapeDataString(amount.ToString("0", CultureInfo.InvariantCulture))}"
            + $"&addInfo={Uri.EscapeDataString(TransferContent(invoiceId))}&accountName={Uri.EscapeDataString(settings.AccountName)}";
    }

    public static async Task<Image> LoadQrAsync(PaymentSettings settings, int invoiceId, decimal amount)
    {
        var url = BuildUrl(settings, invoiceId, amount);
        Log.Information("Tạo QR cho hóa đơn {MaHD}, số tiền {Amount}", invoiceId, amount);
        try
        {
            var bytes = await Client.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);
            using var source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch (Exception ex)
        {
            // HttpRequestException có thể chứa URL tài khoản: chỉ ghi loại lỗi.
            Log.Error("Tải QR thất bại cho hóa đơn {MaHD}: {ErrorType}", invoiceId, ex.GetType().Name);
            throw;
        }
    }
}
