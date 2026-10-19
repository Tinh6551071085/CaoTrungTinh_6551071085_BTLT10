using System.Data;
using System.Text.RegularExpressions;
using Cau2.Data;
using Cau2.Models;
using Cau2.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Cau2;

public partial class Form1 : Form
{
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? wardRequest;
    private List<PhongTro> rooms = [];
    private List<KhachThue> customers = [];
    private List<AddressItem> provinces = [];
    private int? selectedCustomerId;
    private bool binding;
    private bool busy;
    private bool selecting;
    private bool loadingProvinces;
    private readonly ComboBox provinceFilter = new();
    private readonly Button clearFilterButton = new();
    private readonly Button errorButton = new();

    public Form1()
    {
        InitializeComponent();
        ConfigureControls();
        Shown += async (_, _) =>
        {
            await RunAsync(RefreshDataAsync);
            await LoadProvincesAsync();
        };
        FormClosed += (_, _) => { lifetime.Cancel(); wardRequest?.Cancel(); };
    }

    private void ConfigureControls()
    {
        phoneTextBox.MaxLength = 15;
        fullNameTextBox.MaxLength = 100;
        birthDatePicker.MaxDate = DateTime.Today;
        birthDatePicker.Value = DateTime.Today.AddYears(-18);
        searchTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        provinceFilter.SetBounds(369, 44, 235, 23);
        provinceFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        var label = new Label { Text = "Tỉnh:", AutoSize = true, Location = new Point(324, 48) };
        clearFilterButton.Text = "Bỏ lọc";
        clearFilterButton.SetBounds(614, 42, 82, 27);
        errorButton.Text = "Gây lỗi thử";
        errorButton.SetBounds(12, 390, 100, 25);
        errorButton.Visible = false;
        errorButton.Click += (_, _) => throw new InvalidOperationException("Lỗi thử để kiểm tra xử lý lỗi toàn cục.");
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.F12)
            {
                errorButton.Visible = !errorButton.Visible;
                e.Handled = true;
            }
        };
        Controls.AddRange([label, provinceFilter, clearFilterButton, errorButton]);
        customerGrid.Top = 78;
        customerGrid.Height -= 29;
        customerGrid.Columns["Hometown"]!.FillWeight = 175;
        customerGrid.CellClick += async (_, e) =>
        {
            if (e.RowIndex >= 0 && !busy && !selecting)
                await RunAsync(() => SelectCustomerAsync(e.RowIndex));
        };
        provinceComboBox.SelectedIndexChanged += async (_, _) =>
        {
            if (!binding && provinceComboBox.SelectedItem is AddressItem province)
                await LoadWardsAsync(province.Code);
        };
        addButton.Click += async (_, _) => await RunAsync(() => SaveCustomerAsync(false));
        editButton.Click += async (_, _) => await RunAsync(() => SaveCustomerAsync(true));
        deleteButton.Click += async (_, _) => await RunAsync(DeleteCustomerAsync);
        resetButton.Click += async (_, _) => { if (!selecting) await RunAsync(RefreshDataAsync); };
        searchButton.Click += (_, _) => ApplyFilters();
        searchTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { ApplyFilters(); e.SuppressKeyPress = true; }
        };
        roomFilterComboBox.SelectedIndexChanged += (_, _) => { if (!binding) ApplyFilters(); };
        provinceFilter.SelectedIndexChanged += (_, _) => { if (!binding) ApplyFilters(); };
        clearFilterButton.Click += (_, _) =>
        {
            binding = true;
            searchTextBox.Clear();
            roomFilterComboBox.SelectedIndex = 0;
            provinceFilter.SelectedIndex = 0;
            binding = false;
            ApplyFilters();
        };
        statusLabel.Text = "Đang kết nối CSDL…";
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy || IsDisposed) return;
        busy = true;
        ToggleActions(false);
        try { await action(); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Log.Error(ex, "Thao tác dữ liệu thất bại");
            if (!IsDisposed)
            {
                statusLabel.Text = "Thao tác chưa hoàn tất. Kiểm tra cấu hình CSDL và thử lại.";
                MessageBox.Show(this, "Không thể hoàn tất thao tác. Vui lòng kiểm tra kết nối CSDL và thử lại. Thông tin đã được ghi lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally { busy = false; if (!IsDisposed) ToggleActions(!selecting); }
    }

    private void ToggleActions(bool enabled)
    {
        addButton.Enabled = editButton.Enabled = deleteButton.Enabled = resetButton.Enabled = enabled;
        customerGrid.Enabled = enabled;
        customerGroup.Enabled = enabled;
    }

    private async Task RefreshDataAsync()
    {
        await using var db = Database.Open();
        rooms = await db.PhongTro.AsNoTracking().OrderBy(r => r.TenPhong).ToListAsync(lifetime.Token);
        customers = await db.KhachThue.AsNoTracking().Include(c => c.MaPhongNavigation)
            .OrderBy(c => c.MaKhach).ToListAsync(lifetime.Token);
        var roomFilterId = (roomFilterComboBox.SelectedItem as Choice)?.Id ?? 0;
        var provinceFilterId = (provinceFilter.SelectedItem as Choice)?.Id ?? 0;
        binding = true;
        BindChoices(roomFilterComboBox, new[] { new Choice(0, "Tất cả") }.Concat(rooms.Select(r => new Choice(r.MaPhong, r.TenPhong))).ToList());
        BindChoices(provinceFilter, new[] { new Choice(0, "Tất cả tỉnh/thành") }.Concat(customers
            .Select(c => new Choice(c.MaTinh, c.TenTinh)).Where(c => c.Id != 0).DistinctBy(c => c.Id)).ToList());
        roomFilterComboBox.SelectedValue = roomFilterId;
        provinceFilter.SelectedValue = provinceFilterId;
        if (provinceFilter.SelectedIndex < 0) provinceFilter.SelectedIndex = 0;
        binding = false;
        ResetEditor();
        ApplyFilters();
    }

    private static void BindChoices(ComboBox combo, List<Choice> values)
    {
        combo.DisplayMember = nameof(Choice.Name);
        combo.ValueMember = nameof(Choice.Id);
        combo.DataSource = values;
    }

    private void BindRooms(int? currentRoom = null)
    {
        BindChoices(roomComboBox, rooms.Where(r => r.TinhTrang == "Trống" || r.MaPhong == currentRoom)
            .Select(r => new Choice(r.MaPhong, $"{r.TenPhong} - {r.GiaThue:N0} đ")).ToList());
        if (currentRoom.HasValue) roomComboBox.SelectedValue = currentRoom.Value;
    }

    private void ResetEditor()
    {
        wardRequest?.Cancel();
        selectedCustomerId = null;
        fullNameTextBox.Clear();
        phoneTextBox.Clear();
        maleRadioButton.Checked = true;
        birthDatePicker.Value = DateTime.Today.AddYears(-18);
        residenceCheckBox.Checked = false;
        BindRooms();
        binding = true;
        provinceComboBox.SelectedIndex = -1;
        provinceComboBox.Text = "";
        wardComboBox.DataSource = null;
        wardComboBox.Items.Clear();
        wardComboBox.Text = "";
        wardComboBox.Enabled = true;
        binding = false;
        customerGrid.ClearSelection();
        customerGrid.CurrentCell = null;
        fullNameTextBox.Focus();
    }

    private void ApplyFilters()
    {
        if (binding || IsDisposed) return;
        var search = searchTextBox.Text.Trim();
        var roomId = (roomFilterComboBox.SelectedItem as Choice)?.Id ?? 0;
        var provinceId = (provinceFilter.SelectedItem as Choice)?.Id ?? 0;
        var visible = customers.Where(c => (search.Length == 0 || c.HoTen.Contains(search, StringComparison.CurrentCultureIgnoreCase)
            || c.SoDienThoai.Contains(search)) && (roomId == 0 || c.MaPhong == roomId)
            && (provinceId == 0 || c.MaTinh == provinceId)).ToList();
        customerGrid.Rows.Clear();
        foreach (var customer in visible)
        {
            var index = customerGrid.Rows.Add(customer.HoTen, customer.SoDienThoai,
                customer.MaPhongNavigation.TenPhong, $"{customer.MaPhongNavigation.GiaThue:N0} đ",
                $"{customer.TenPhuongXa} – {customer.TenTinh}", customer.DaDangKyTamTru);
            customerGrid.Rows[index].Tag = customer;
        }
        customerGrid.ClearSelection();
        customerGrid.CurrentCell = null;
        statusLabel.Text = $"Hiển thị {visible.Count}/{customers.Count} khách thuê";
    }

    private async Task SelectCustomerAsync(int index)
    {
        if (customerGrid.Rows[index].Tag is not KhachThue customer) return;
        selecting = true;
        try
        {
            selectedCustomerId = customer.MaKhach;
            fullNameTextBox.Text = customer.HoTen;
            phoneTextBox.Text = customer.SoDienThoai;
            maleRadioButton.Checked = customer.GioiTinh == "Nam";
            femaleRadioButton.Checked = !maleRadioButton.Checked;
            birthDatePicker.Value = customer.NgaySinh.ToDateTime(TimeOnly.MinValue);
            residenceCheckBox.Checked = customer.DaDangKyTamTru;
            BindRooms(customer.MaPhong);
            binding = true;
            var addresses = provinces.ToList();
            if (!addresses.Any(p => p.Code == customer.MaTinh && p.Name == customer.TenTinh))
                addresses.Add(new AddressItem { Code = customer.MaTinh, Name = customer.TenTinh });
            BindAddresses(provinceComboBox, addresses);
            provinceComboBox.SelectedItem = addresses.First(p => p.Code == customer.MaTinh && p.Name == customer.TenTinh);
            binding = false;
            await LoadWardsAsync(customer.MaTinh, customer.MaPhuongXa, customer.TenPhuongXa);
        }
        finally { binding = false; selecting = false; }
    }

    private static void BindAddresses(ComboBox combo, List<AddressItem> values)
    {
        combo.DisplayMember = nameof(AddressItem.Name);
        combo.ValueMember = nameof(AddressItem.Code);
        combo.DataSource = values;
    }

    private async Task LoadProvincesAsync()
    {
        loadingProvinces = true;
        provinceComboBox.Enabled = false;
        ToggleActions(false);
        statusLabel.Text = "Đang tải danh sách tỉnh/thành…";
        try
        {
            provinces = await AddressService.GetProvincesAsync(lifetime.Token);
            binding = true;
            BindAddresses(provinceComboBox, provinces);
            provinceComboBox.SelectedIndex = -1;
            statusLabel.Text = $"Đã tải {provinces.Count} tỉnh/thành từ provinces.open-api.vn";
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            if (!IsDisposed)
            {
                provinceComboBox.DropDownStyle = ComboBoxStyle.DropDown;
                wardComboBox.DropDownStyle = ComboBoxStyle.DropDown;
                ReportAddressError(ex);
            }
        }
        finally
        {
            loadingProvinces = false;
            binding = false;
            if (!IsDisposed) { provinceComboBox.Enabled = true; ToggleActions(!busy); }
        }
    }

    private async Task LoadWardsAsync(int provinceId, int wardId = 0, string? savedName = null)
    {
        wardRequest?.Cancel();
        var request = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        wardRequest = request;
        wardComboBox.DataSource = null;
        wardComboBox.Items.Clear();
        wardComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        wardComboBox.Text = "Đang tải…";
        wardComboBox.Enabled = false;
        try
        {
            var wards = provinceId == 0 ? new List<AddressItem>() : await AddressService.GetWardsAsync(provinceId, request.Token);
            if (request.IsCancellationRequested || IsDisposed) return;
            if (savedName is not null && !wards.Any(w => w.Code == wardId && w.Name == savedName))
                wards.Add(new AddressItem { Code = wardId, Name = savedName });
            BindAddresses(wardComboBox, wards);
            wardComboBox.DropDownStyle = wards.Count > 0 ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
            wardComboBox.SelectedIndex = -1;
            wardComboBox.Text = "";
            if (savedName is not null) wardComboBox.SelectedItem = wards.First(w => w.Code == wardId && w.Name == savedName);
            statusLabel.Text = $"Đã tải {wards.Count} phường/xã";
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            if (!request.IsCancellationRequested && !IsDisposed)
            {
                if (savedName is not null)
                    BindAddresses(wardComboBox, [new AddressItem { Code = wardId, Name = savedName }]);
                wardComboBox.Text = savedName ?? "";
                ReportAddressError(ex);
            }
        }
        finally
        {
            if (ReferenceEquals(wardRequest, request))
            {
                wardRequest = null;
                if (!IsDisposed) { wardComboBox.Enabled = true; if (wardComboBox.Text == "Đang tải…") wardComboBox.Text = ""; }
            }
            request.Dispose();
        }
    }

    private void ReportAddressError(Exception ex)
    {
        Log.Warning(ex, "Không tải được địa chỉ");
        statusLabel.Text = "API không phản hồi. Có thể nhập địa chỉ thủ công.";
        MessageBox.Show(this, "Không tải được danh sách địa chỉ, vui lòng kiểm tra mạng. Bạn có thể nhập tay địa chỉ để tiếp tục.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task SaveCustomerAsync(bool editing)
    {
        if (editing && selectedCustomerId is null) { Notify("Vui lòng chọn khách cần sửa."); return; }
        if (!editing && selectedCustomerId is not null) { Notify("Bấm Làm mới trước khi thêm khách."); return; }
        if (string.IsNullOrWhiteSpace(fullNameTextBox.Text)) { Notify("Vui lòng nhập họ tên."); return; }
        if (!Regex.IsMatch(phoneTextBox.Text.Trim(), @"^\+?[0-9]{9,14}$")) { Notify("Số điện thoại phải có 9–14 chữ số, có thể bắt đầu bằng +."); return; }
        if (roomComboBox.SelectedItem is not Choice room) { Notify("Vui lòng chọn phòng trống hợp lệ."); return; }
        if (loadingProvinces || wardRequest is not null) { Notify("Vui lòng chờ tải địa chỉ."); return; }
        var provinceName = provinceComboBox.Text.Trim();
        var wardName = wardComboBox.Text.Trim();
        if (provinceName.Length is 0 or > 100 || wardName.Length is 0 or > 100) { Notify("Vui lòng nhập tỉnh/thành và phường/xã (tối đa 100 ký tự)."); return; }
        await using var db = Database.Open();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, lifetime.Token);
        var customer = editing ? await db.KhachThue.SingleAsync(c => c.MaKhach == selectedCustomerId, lifetime.Token) : new KhachThue();
        var oldRoomId = editing ? customer.MaPhong : (int?)null;
        var targetRoom = await db.PhongTro.SingleAsync(r => r.MaPhong == room.Id, lifetime.Token);
        if (oldRoomId != room.Id && (targetRoom.TinhTrang != "Trống" || await db.KhachThue.AnyAsync(c => c.MaPhong == room.Id, lifetime.Token)))
        { Notify("Phòng đã có khách thuê. Vui lòng làm mới và chọn phòng khác."); return; }
        customer.HoTen = fullNameTextBox.Text.Trim();
        customer.GioiTinh = maleRadioButton.Checked ? "Nam" : "Nữ";
        customer.NgaySinh = DateOnly.FromDateTime(birthDatePicker.Value);
        customer.SoDienThoai = phoneTextBox.Text.Trim();
        customer.MaTinh = provinceComboBox.SelectedItem is AddressItem selectedProvince && selectedProvince.Name == provinceName ? selectedProvince.Code : 0;
        customer.TenTinh = provinceName;
        customer.MaPhuongXa = wardComboBox.SelectedItem is AddressItem selectedWard && selectedWard.Name == wardName ? selectedWard.Code : 0;
        customer.TenPhuongXa = wardName;
        customer.DaDangKyTamTru = residenceCheckBox.Checked;
        customer.MaPhong = room.Id;
        if (!editing) db.KhachThue.Add(customer);
        targetRoom.TinhTrang = "Đang thuê";
        await db.SaveChangesAsync(lifetime.Token);
        if (oldRoomId.HasValue && oldRoomId != room.Id)
            await UpdateRoomStatusAsync(db, oldRoomId.Value);
        await transaction.CommitAsync(lifetime.Token);
        await RefreshDataAsync();
        statusLabel.Text = editing ? "Đã cập nhật khách thuê." : "Đã thêm khách thuê.";
    }

    private async Task DeleteCustomerAsync()
    {
        if (selectedCustomerId is null) { Notify("Vui lòng chọn khách cần xóa."); return; }
        if (MessageBox.Show(this, $"Xóa khách {fullNameTextBox.Text}?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await using var db = Database.Open();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, lifetime.Token);
        var customer = await db.KhachThue.SingleAsync(c => c.MaKhach == selectedCustomerId, lifetime.Token);
        var roomId = customer.MaPhong;
        db.KhachThue.Remove(customer);
        await db.SaveChangesAsync(lifetime.Token);
        await UpdateRoomStatusAsync(db, roomId);
        await transaction.CommitAsync(lifetime.Token);
        await RefreshDataAsync();
        statusLabel.Text = "Đã xóa khách thuê.";
    }

    private async Task UpdateRoomStatusAsync(QlNhaTroContext db, int roomId)
    {
        var room = await db.PhongTro.SingleAsync(r => r.MaPhong == roomId, lifetime.Token);
        room.TinhTrang = await db.KhachThue.AnyAsync(c => c.MaPhong == roomId, lifetime.Token) ? "Đang thuê" : "Trống";
        await db.SaveChangesAsync(lifetime.Token);
    }

    private void Notify(string message) => MessageBox.Show(this, message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private sealed record Choice(int Id, string Name);
}
