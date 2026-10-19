using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Cau1.Data;
using Cau1.Models;

namespace Cau1;

public partial class Form1 : Form
{
    private readonly DbContextOptions<ChiTieuContext>? _options;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("vi-VN");
    private int? _selectedId;
    private bool _busy;

    // Constructor không truy cập CSDL để Visual Studio Designer mở được Form1.
    public Form1()
    {
        InitializeComponent();
    }

    public Form1(DbContextOptions<ChiTieuContext> options) : this()
    {
        _options = options;
        dgvChiTieu.DefaultCellStyle.FormatProvider = _culture;
        dgvChiTieu.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgvChiTieu.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 215);
        dgvChiTieu.DefaultCellStyle.SelectionForeColor = Color.White;
        colMa.DefaultCellStyle.Format = "000";
        colMa.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colNgayChi.DefaultCellStyle.Format = "dd/MM/yyyy";
        colNgayChi.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colNoiDung.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        colSoTien.DefaultCellStyle.Format = "N0";
        colSoTien.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        ResetFilters();
        ResetEditor();
        Shown += async (_, _) => await PerformAsync(LoadExpensesAsync);
        btnTim.Click += async (_, _) => await SearchAsync();
        txtTimKiem.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await SearchAsync();
        };
        btnThem.Click += async (_, _) => await SaveAsync(true);
        btnSua.Click += async (_, _) => await SaveAsync(false);
        btnXoa.Click += async (_, _) => await DeleteAsync();
        btnLamMoi.Click += async (_, _) =>
        {
            ResetFilters();
            await PerformAsync(async () => { await LoadExpensesAsync(); ResetEditor(); });
        };
        dgvChiTieu.SelectionChanged += (_, _) => ShowSelectedExpense();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
    }

    private void ResetFilters()
    {
        txtTimKiem.Clear();
        cboLocLoai.SelectedIndex = 0;
        dtpTuNgay.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        dtpDenNgay.Value = DateTime.Today;
    }

    private void ResetEditor()
    {
        _selectedId = null;
        dtpNgayChi.Value = DateTime.Today;
        txtNoiDung.Clear();
        cboLoaiChi.SelectedIndex = 0;
        nudSoTien.Value = 0;
        txtGhiChu.Clear();
        dgvChiTieu.ClearSelection();
        UpdateButtons();
    }

    private void ShowSelectedExpense()
    {
        if (dgvChiTieu.SelectedRows.Count == 0 ||
            dgvChiTieu.SelectedRows[0].DataBoundItem is not ChiTieu item)
        {
            _selectedId = null;
            UpdateButtons();
            return;
        }
        _selectedId = item.MaCt;
        dtpNgayChi.Value = item.NgayChi.ToDateTime(TimeOnly.MinValue);
        txtNoiDung.Text = item.NoiDung;
        if (!cboLoaiChi.Items.Contains(item.LoaiChi)) cboLoaiChi.Items.Add(item.LoaiChi);
        cboLoaiChi.SelectedItem = item.LoaiChi;
        nudSoTien.Value = Math.Clamp(item.SoTien, nudSoTien.Minimum, nudSoTien.Maximum);
        txtGhiChu.Text = item.GhiChu ?? "";
        UpdateButtons();
    }

    private async Task SearchAsync()
    {
        if (dtpTuNgay.Value.Date > dtpDenNgay.Value.Date)
        {
            Notify("Từ ngày không được lớn hơn đến ngày.");
            dtpTuNgay.Focus();
            return;
        }
        await PerformAsync(LoadExpensesAsync);
    }

    private async Task LoadExpensesAsync()
    {
        var from = DateOnly.FromDateTime(dtpTuNgay.Value);
        var to = DateOnly.FromDateTime(dtpDenNgay.Value);
        var keyword = txtTimKiem.Text.Trim();
        var category = cboLocLoai.SelectedItem?.ToString();
        await using var db = new ChiTieuContext(_options!);
        var query = db.ChiTieus.AsNoTracking().Where(x => x.NgayChi >= from && x.NgayChi <= to);
        if (keyword.Length > 0) query = query.Where(x => x.NoiDung.Contains(keyword));
        if (category is not null && category != "Tất cả") query = query.Where(x => x.LoaiChi == category);
        var items = await query.OrderBy(x => x.NgayChi).ThenBy(x => x.MaCt).ToListAsync();
        dgvChiTieu.DataSource = items;
        lblTongChi.Text = $"Tổng chi: {items.Sum(x => x.SoTien).ToString("N0", _culture)} đ";
        if (items.Count == 0) ResetEditor();
    }

    private bool ValidateExpense()
    {
        if (string.IsNullOrWhiteSpace(txtNoiDung.Text))
        {
            Notify("Vui lòng nhập nội dung khoản chi.");
            txtNoiDung.Focus();
            return false;
        }
        if (nudSoTien.Value <= 0)
        {
            Notify("Số tiền phải lớn hơn 0.");
            nudSoTien.Focus();
            return false;
        }
        if (dtpNgayChi.Value.Date > DateTime.Today)
        {
            Notify("Ngày chi không được lớn hơn hôm nay.");
            dtpNgayChi.Focus();
            return false;
        }
        if (cboLoaiChi.SelectedItem is null)
        {
            Notify("Vui lòng chọn loại chi.");
            return false;
        }
        return true;
    }

    private async Task SaveAsync(bool isNew)
    {
        if (_busy || (!isNew && !_selectedId.HasValue) || !ValidateExpense()) return;
        await PerformAsync(async () =>
        {
            await using var db = new ChiTieuContext(_options!);
            var item = isNew ? new ChiTieu() : await db.ChiTieus.FindAsync(_selectedId!.Value);
            if (item is null)
            {
                Notify("Khoản chi này không còn tồn tại. Vui lòng tải lại danh sách.");
                await LoadExpensesAsync();
                return;
            }
            item.NgayChi = DateOnly.FromDateTime(dtpNgayChi.Value);
            item.NoiDung = txtNoiDung.Text.Trim();
            item.LoaiChi = cboLoaiChi.SelectedItem!.ToString()!;
            item.SoTien = nudSoTien.Value;
            item.GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim();
            if (isNew) db.ChiTieus.Add(item);
            await db.SaveChangesAsync();
            Log.Information("{ThaoTac} khoản chi MaCT={MaCT}", isNew ? "Thêm" : "Sửa", item.MaCt);
            await LoadExpensesAsync();
            ResetEditor();
        });
    }

    private async Task DeleteAsync()
    {
        if (_busy || !_selectedId.HasValue) return;
        var id = _selectedId.Value;
        if (MessageBox.Show(this, $"Bạn có chắc muốn xóa khoản chi mã {id:000}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;
        await PerformAsync(async () =>
        {
            await using var db = new ChiTieuContext(_options!);
            var item = await db.ChiTieus.FindAsync(id);
            if (item is not null)
            {
                db.ChiTieus.Remove(item);
                await db.SaveChangesAsync();
                Log.Information("Xóa khoản chi MaCT={MaCT}", id);
            }
            else Notify("Khoản chi này đã được xóa trước đó.");
            await LoadExpensesAsync();
            ResetEditor();
        });
    }

    private async Task PerformAsync(Func<Task> action)
    {
        if (_busy) return;
        _busy = true;
        SetBusy(true);
        try { await action(); }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi truy cập cơ sở dữ liệu; MaCT={MaCT}", _selectedId);
            MessageBox.Show(this, "Không thể đọc hoặc lưu dữ liệu. Vui lòng kiểm tra SQL Server và " +
                "connection string trong appsettings.json. Chi tiết lỗi được ghi trong thư mục Logs.",
                "Lỗi dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        grpKhoanChi.Enabled = grpTimKiem.Enabled = dgvChiTieu.Enabled = !busy;
        btnThem.Enabled = btnLamMoi.Enabled = !busy;
        UpdateButtons();
    }

    private void UpdateButtons() => btnSua.Enabled = btnXoa.Enabled = !_busy && _selectedId.HasValue;

    private void Notify(string message) => MessageBox.Show(this, message, "Sổ chi tiêu cá nhân",
        MessageBoxButtons.OK, MessageBoxIcon.Information);
}
