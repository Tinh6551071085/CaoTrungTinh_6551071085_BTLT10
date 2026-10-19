using Microsoft.EntityFrameworkCore;

namespace Cau4;

public sealed class FormHoaDon : Form
{
    private readonly DateTimePicker day = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 135 };
    private readonly Label revenue = new() { Text = "Doanh thu ngày: 0 đ", AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Margin = new Padding(20, 7, 0, 0) };
    private readonly DataGridView invoices = TeaUi.Grid();
    private readonly DataGridView details = TeaUi.Grid();
    private bool loading;
    private int detailRequest;
    private sealed record InvoiceRow(int MaHd, DateTime NgayLap, string HinhThucTt, decimal TongTien);

    public FormHoaDon()
    {
        TeaUi.Setup(this, "Danh sách hóa đơn", new Size(950, 680));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 33)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "Danh sách hóa đơn", AutoSize = true, Font = new Font(Font.FontFamily, 13, FontStyle.Bold) });
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill };
        filters.Controls.Add(new Label { Text = "Ngày lập", AutoSize = true, Margin = new Padding(0, 8, 8, 0) }); filters.Controls.Add(day);
        filters.Controls.Add(TeaUi.Button("Lọc / Làm mới", LoadInvoicesAsync)); filters.Controls.Add(revenue); layout.Controls.Add(filters);
        TeaUi.Column(invoices, "MaHd", "Mã HD"); TeaUi.Column(invoices, "NgayLap", "Giờ lập"); TeaUi.Column(invoices, "HinhThucTt", "Hình thức"); TeaUi.Column(invoices, "TongTien", "Tổng tiền", true);
        invoices.Columns["NgayLap"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss"; layout.Controls.Add(invoices);
        layout.Controls.Add(new Label { Text = "Chi tiết hóa đơn đang chọn", AutoSize = true, Margin = new Padding(0, 8, 0, 0), Font = new Font(Font, FontStyle.Bold) });
        TeaUi.Column(details, "TenMon", "Tên món"); TeaUi.Column(details, "SoLy", "Số ly"); TeaUi.Column(details, "DonGia", "Đơn giá", true); TeaUi.Column(details, "ThanhTien", "Thành tiền", true); layout.Controls.Add(details);
        invoices.SelectionChanged += async (_, _) => { if (loading) return; try { await LoadDetailsAsync(); } catch (Exception ex) { TeaUi.Error(this, ex); } };
        Shown += async (_, _) => { try { await LoadInvoicesAsync(); } catch (Exception ex) { TeaUi.Error(this, ex); } };
    }

    private async Task LoadInvoicesAsync()
    {
        var from = day.Value.Date; var until = from.AddDays(1);
        await using var db = TeaDatabase.Open();
        var rows = await db.HoaDons.AsNoTracking().Where(x => x.NgayLap >= from && x.NgayLap < until && x.DaThanhToan)
            .OrderByDescending(x => x.NgayLap).Select(x => new InvoiceRow(x.MaHd, x.NgayLap, x.HinhThucTt, x.TongTien)).ToListAsync();
        loading = true; try { invoices.DataSource = rows; } finally { loading = false; }
        revenue.Text = "Doanh thu ngày: " + TeaUi.Money(rows.Sum(x => x.TongTien));
        await LoadDetailsAsync();
    }

    private async Task LoadDetailsAsync()
    {
        var request = ++detailRequest;
        if (invoices.CurrentRow?.DataBoundItem is not InvoiceRow row) { details.DataSource = null; return; }
        await using var db = TeaDatabase.Open();
        var rows = await (from detail in db.ChiTietHoaDons.AsNoTracking()
                          join item in db.MonUongs on detail.MaMon equals item.MaMon
                          where detail.MaHd == row.MaHd
                          select new { item.TenMon, detail.SoLy, detail.DonGia, ThanhTien = detail.SoLy * detail.DonGia }).ToListAsync();
        if (!IsDisposed && request == detailRequest) details.DataSource = rows;
    }
}
