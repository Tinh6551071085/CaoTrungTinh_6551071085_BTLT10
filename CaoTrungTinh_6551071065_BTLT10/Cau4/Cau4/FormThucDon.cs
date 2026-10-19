using Microsoft.EntityFrameworkCore;

namespace Cau4;

public sealed class FormThucDon : Form
{
    private readonly TextBox name = new() { MaxLength = 100 };
    private readonly ComboBox category = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown price = new() { Minimum = 1, Maximum = 999999999, ThousandsSeparator = true, Increment = 1000, Value = 25000 };
    private readonly CheckBox selling = new() { Text = "Đang bán", Checked = true, AutoSize = true };
    private readonly TextBox search = new() { Width = 240, PlaceholderText = "Tìm tên món..." };
    private readonly ComboBox filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
    private readonly DataGridView grid = TeaUi.Grid();
    private int? selectedId;
    private bool loading;
    private sealed record CategoryOption(int Id, string Name);
    private sealed record MenuRow(int MaMon, string TenMon, int MaDm, string TenDm, decimal GiaBan, bool DangBan);

    public FormThucDon()
    {
        TeaUi.Setup(this, "Quản lý thực đơn", new Size(900, 650));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 204));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 37)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "Quản lý thực đơn", Font = new Font(Font.FontFamily, 13, FontStyle.Bold), AutoSize = true });
        var box = new GroupBox { Text = "Thông tin món", Dock = DockStyle.Fill };
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, Height = 135, Padding = new Padding(8, 2, 8, 0), ColumnCount = 2, RowCount = 4 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        TeaUi.Field(fields, 0, "Tên món", name); TeaUi.Field(fields, 1, "Danh mục", category);
        TeaUi.Field(fields, 2, "Giá bán (đ)", price); TeaUi.Field(fields, 3, "Trạng thái", selling); box.Controls.Add(fields);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(8, 0, 0, 0) };
        actions.Controls.Add(TeaUi.Button("Thêm", () => SaveAsync(false))); actions.Controls.Add(TeaUi.Button("Sửa", () => SaveAsync(true)));
        actions.Controls.Add(TeaUi.Button("Xóa", DeleteAsync)); actions.Controls.Add(TeaUi.Button("Làm mới", async () => { search.Clear(); filter.SelectedIndex = 0; await LoadRowsAsync(); ClearEditor(); })); box.Controls.Add(actions); layout.Controls.Add(box);
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        filters.Controls.Add(search); filters.Controls.Add(filter); filters.Controls.Add(TeaUi.Button("Tìm", LoadRowsAsync)); layout.Controls.Add(filters);
        TeaUi.Column(grid, "MaMon", "Mã món"); TeaUi.Column(grid, "TenMon", "Tên món"); TeaUi.Column(grid, "TenDm", "Danh mục"); TeaUi.Column(grid, "GiaBan", "Giá bán", true);
        grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "DangBan", HeaderText = "Đang bán" }); layout.Controls.Add(grid);
        grid.SelectionChanged += (_, _) =>
        {
            if (loading || grid.CurrentRow?.DataBoundItem is not MenuRow row) return;
            selectedId = row.MaMon; name.Text = row.TenMon; category.SelectedValue = row.MaDm;
            price.Value = row.GiaBan; selling.Checked = row.DangBan;
        };
        Shown += async (_, _) =>
        {
            try
            {
                await using var db = TeaDatabase.Open();
                var categories = await db.DanhMucMons.AsNoTracking().OrderBy(x => x.MaDm).Select(x => new CategoryOption(x.MaDm, x.TenDm)).ToListAsync();
                category.DisplayMember = filter.DisplayMember = "Name"; category.ValueMember = filter.ValueMember = "Id";
                category.DataSource = categories;
                filter.DataSource = new[] { new CategoryOption(0, "Tất cả danh mục") }.Concat(categories).ToList();
                await LoadRowsAsync(); ClearEditor();
            }
            catch (Exception ex) { TeaUi.Error(this, ex); }
        };
        filter.SelectedIndexChanged += async (_, _) => { if (!Visible) return; try { await LoadRowsAsync(); } catch (Exception ex) { TeaUi.Error(this, ex); } };
    }

    private async Task LoadRowsAsync()
    {
        await using var db = TeaDatabase.Open();
        var categoryId = filter.SelectedValue is int id ? id : 0;
        var term = search.Text.Trim();
        var rows = await (from item in db.MonUongs.AsNoTracking()
                          join groupItem in db.DanhMucMons on item.MaDm equals groupItem.MaDm
                          where (term == "" || item.TenMon.Contains(term)) && (categoryId == 0 || item.MaDm == categoryId)
                          orderby item.MaMon
                          select new MenuRow(item.MaMon, item.TenMon, item.MaDm, groupItem.TenDm, item.GiaBan, item.DangBan)).ToListAsync();
        loading = true; try { grid.DataSource = rows; grid.ClearSelection(); selectedId = null; } finally { loading = false; }
    }

    private void ClearEditor()
    {
        selectedId = null; grid.ClearSelection(); name.Clear(); price.Value = 25000; selling.Checked = true;
        if (category.Items.Count > 0) category.SelectedIndex = 0; name.Focus();
    }

    private async Task SaveAsync(bool edit)
    {
        if (edit && selectedId is null) { MessageBox.Show(this, "Chọn món cần sửa."); return; }
        if (string.IsNullOrWhiteSpace(name.Text) || category.SelectedValue is not int categoryId)
        { MessageBox.Show(this, "Nhập tên món và chọn danh mục."); return; }
        await using var db = TeaDatabase.Open();
        var item = edit ? await db.MonUongs.FindAsync(selectedId!.Value) : new MonUong();
        if (item is null) { MessageBox.Show(this, "Món không còn tồn tại. Hãy làm mới danh sách."); return; }
        item.TenMon = name.Text.Trim(); item.MaDm = categoryId; item.GiaBan = price.Value; item.DangBan = selling.Checked;
        if (!edit) db.MonUongs.Add(item);
        await db.SaveChangesAsync(); await LoadRowsAsync(); ClearEditor();
    }

    private async Task DeleteAsync()
    {
        if (selectedId is not int id) { MessageBox.Show(this, "Chọn món cần xóa."); return; }
        await using var db = TeaDatabase.Open();
        if (await db.ChiTietHoaDons.AnyAsync(x => x.MaMon == id))
        { MessageBox.Show(this, "Món đã có trong hóa đơn, không thể xóa. Hãy bỏ chọn Đang bán để ngừng bán."); return; }
        if (MessageBox.Show(this, "Xóa món đang chọn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var item = await db.MonUongs.FindAsync(id);
        if (item is not null) { db.MonUongs.Remove(item); await db.SaveChangesAsync(); }
        await LoadRowsAsync(); ClearEditor();
    }
}
