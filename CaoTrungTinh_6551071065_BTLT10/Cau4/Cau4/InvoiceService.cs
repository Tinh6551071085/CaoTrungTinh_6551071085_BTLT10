using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Cau4;

internal static class InvoiceService
{
    public static async Task<HoaDon> SaveAsync(int id, IReadOnlyList<CartItem> items, string method)
    {
        if (id <= 0 || items.Count == 0 || items.Any(x => x.SoLy <= 0 || x.DonGia <= 0)
            || items.Select(x => x.MaMon).Distinct().Count() != items.Count
            || method is not ("Tiền mặt" or "Chuyển khoản")) throw new InvalidOperationException("Hóa đơn không hợp lệ.");
        await using var db = TeaDatabase.Open();
        var total = items.Sum(x => x.ThanhTien);
        var existing = await db.HoaDons.AsNoTracking().Include(x => x.ChiTietHoaDons).SingleOrDefaultAsync(x => x.MaHd == id);
        if (existing is not null)
        {
            // Thử lại sau lỗi mạng không tạo hóa đơn trùng hoặc ghi đè đơn đã lưu.
            if (existing.DaThanhToan && existing.TongTien == total && existing.ChiTietHoaDons.Count == items.Count &&
                items.All(item => existing.ChiTietHoaDons.Any(x => x.MaMon == item.MaMon && x.SoLy == item.SoLy && x.DonGia == item.DonGia))) return existing;
            throw new InvalidOperationException("Mã hóa đơn đã được sử dụng cho đơn khác.");
        }
        var invoice = new HoaDon
        {
            MaHd = id, NgayLap = DateTime.Now, TongTien = total, HinhThucTt = method, DaThanhToan = true,
            ChiTietHoaDons = items.Select(x => new ChiTietHoaDon { MaMon = x.MaMon, SoLy = x.SoLy, DonGia = x.DonGia }).ToList()
        };
        db.HoaDons.Add(invoice);
        // EF Core dùng transaction cho hóa đơn và toàn bộ chi tiết trong một SaveChanges.
        await db.SaveChangesAsync();
        Log.Information("Đã lưu hóa đơn {MaHD}, {Amount}, {Method}", id, total, method);
        return invoice;
    }
}
