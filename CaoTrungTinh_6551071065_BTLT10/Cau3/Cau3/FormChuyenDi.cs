using System.Data;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.Json;
using Cau3.Models;
using Cau3.Services;

namespace Cau3;

public partial class FormChuyenDi : Form
{
    private TripService? trips;
    private readonly WeatherService weatherService;
    private CancellationTokenSource? weatherCancellation;
    private WeatherForecast? forecast;
    private string weatherState = "idle", status = "starting";
    private bool databaseBusy, databaseReady, loadingEditor, closing;
    private int rowCount;
    private bool English => cboLanguage.SelectedIndex == 1;
    private string T(string vi, string en) => English ? en : vi;
    private ChuyenDi? Selected => grid.CurrentRow?.DataBoundItem as ChuyenDi;

    public FormChuyenDi() : this(new WeatherService()) { }

    public FormChuyenDi(WeatherService weatherService)
    {
        this.weatherService = weatherService;
        InitializeComponent();
        cboLanguage.SelectedIndex = AppSettings.LoadLanguage().Language == "en-US" ? 1 : 0;
        ApplyLanguage();
        grid.SelectionChanged += (_, _) => LoadSelected();
        grid.CellFormatting += FormatTransport;
        btnSearch.Click += async (_, _) => await ReloadAsync();
        txtSearch.TextChanged += (_, _) => ScheduleSearch();
        chkPending.CheckedChanged += (_, _) => ScheduleSearch();
        dtpFrom.ValueChanged += (_, _) => ScheduleSearch();
        dtpTo.ValueChanged += (_, _) => ScheduleSearch();
        searchTimer.Tick += async (_, _) => { searchTimer.Stop(); await ReloadAsync(); };
        btnReset.Click += async (_, _) =>
        {
            if (!databaseReady) { await InitializeDatabaseAsync(); return; }
            txtSearch.Clear(); chkPending.Checked = false;
            dtpFrom.Checked = false; dtpTo.Checked = false;
            searchTimer.Stop();
            await ReloadAsync();
            ResetEditor();
        };
        btnAdd.Click += async (_, _) => await SaveTripAsync(false);
        btnEdit.Click += async (_, _) => await SaveTripAsync(true);
        btnDelete.Click += async (_, _) => await DeleteTripAsync();
        btnWeather.Click += async (_, _) => await FetchWeatherAsync();
        txtPlace.TextChanged += (_, _) => { if (!loadingEditor) ClearWeather(); };
        dtpDate.ValueChanged += (_, _) => { if (!loadingEditor) ClearWeather(); };
        cboLanguage.SelectedIndexChanged += (_, _) =>
        {
            ApplyLanguage();
            try { AppSettings.SaveLanguage(English ? "en-US" : "vi-VN"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { ShowError("settings"); }
        };
        Shown += async (_, _) => await InitializeDatabaseAsync();
        FormClosing += (_, _) => { closing = true; searchTimer.Stop(); weatherCancellation?.Cancel(); };
    }

    private async Task InitializeDatabaseAsync()
    {
        if (databaseBusy) return;
        await RunDatabaseAsync(async () =>
        {
            trips = new TripService(AppSettings.ConnectionString());
            await trips.InitializeAsync();
            databaseReady = true;
            await BindRowsAsync();
        });
    }

    private async Task RunDatabaseAsync(Func<Task> action)
    {
        if (databaseBusy || closing) return;
        searchTimer.Stop();
        databaseBusy = true; status = "loading"; UpdateStatus(); UpdateButtons();
        try { await action(); if (!closing) status = "count"; }
        catch (DBConcurrencyException) { if (!closing) { status = "databaseError"; ShowError("concurrency"); } }
        catch (Exception) { if (!closing) { status = "databaseError"; ShowError("database"); } }
        finally
        {
            databaseBusy = false;
            if (!closing) { UpdateStatus(); UpdateButtons(); }
        }
    }

    private void ScheduleSearch()
    {
        if (!databaseReady || databaseBusy) return;
        searchTimer.Stop(); searchTimer.Start();
    }

    private bool ValidRange()
    {
        if (!dtpFrom.Checked || !dtpTo.Checked || dtpFrom.Value.Date <= dtpTo.Value.Date) return true;
        ShowError("range"); return false;
    }

    private async Task ReloadAsync()
    {
        if (!databaseReady || !ValidRange()) return;
        await RunDatabaseAsync(() => BindRowsAsync(Selected?.MaChuyen));
    }

    private async Task BindRowsAsync(int? selectedId = null)
    {
        var rows = await trips!.SearchAsync(txtSearch.Text.Trim(), chkPending.Checked,
            dtpFrom.Checked ? DateOnly.FromDateTime(dtpFrom.Value) : null,
            dtpTo.Checked ? DateOnly.FromDateTime(dtpTo.Value) : null);
        if (closing) return;
        grid.DataSource = rows;
        rowCount = rows.Count;
        if (rows.Count == 0) ResetEditor();
        else if (selectedId.HasValue)
            foreach (DataGridViewRow row in grid.Rows)
                if (row.DataBoundItem is ChuyenDi trip && trip.MaChuyen == selectedId) { grid.CurrentCell = row.Cells[0]; break; }
        LoadSelected();
    }

    private void LoadSelected()
    {
        if (Selected is not { } trip) { UpdateButtons(); return; }
        loadingEditor = true;
        txtName.Text = trip.TenChuyen;
        txtPlace.Text = trip.DiaDiem;
        dtpDate.Value = trip.NgayDi.ToDateTime(TimeOnly.MinValue);
        nudPeople.Value = trip.SoNguoi;
        nudBudget.Value = trip.NganSach;
        cboTransport.SelectedIndex = Array.IndexOf(TripService.Transports, trip.PhuongTien);
        chkDone.Checked = trip.DaHoanThanh;
        txtNotes.Text = trip.GhiChu;
        loadingEditor = false;
        ClearWeather(); UpdateButtons();
    }

    private void ResetEditor()
    {
        grid.CurrentCell = null; grid.ClearSelection();
        loadingEditor = true;
        txtName.Clear(); txtPlace.Clear(); txtNotes.Clear();
        dtpDate.Value = DateTime.Today;
        nudPeople.Value = 1; nudBudget.Value = 0;
        cboTransport.SelectedIndex = 1; chkDone.Checked = false;
        loadingEditor = false;
        ClearWeather(); UpdateButtons(); txtName.Focus();
    }

    private async Task SaveTripAsync(bool edit)
    {
        if (edit && Selected is null) { ShowError("select"); return; }
        var trip = new ChuyenDi
        {
            MaChuyen = edit ? Selected!.MaChuyen : 0,
            TenChuyen = txtName.Text.Trim(), DiaDiem = txtPlace.Text.Trim(),
            NgayDi = DateOnly.FromDateTime(dtpDate.Value), SoNguoi = (int)nudPeople.Value,
            NganSach = nudBudget.Value, PhuongTien = cboTransport.SelectedIndex < 0 ? "" : TripService.Transports[cboTransport.SelectedIndex],
            DaHoanThanh = chkDone.Checked, GhiChu = string.IsNullOrWhiteSpace(txtNotes.Text) ? null : txtNotes.Text.Trim()
        };
        string? error = TripService.Validate(trip, !edit, DateOnly.FromDateTime(DateTime.Today));
        if (error is not null) { ShowError(error); return; }
        if (!ValidRange()) return;
        await RunDatabaseAsync(async () => { await trips!.SaveAsync(trip, !edit); await BindRowsAsync(trip.MaChuyen); });
    }

    private async Task DeleteTripAsync()
    {
        if (Selected is not { } trip) { ShowError("select"); return; }
        if (MessageBox.Show(this, T("Xóa chuyến đi đang chọn?", "Delete the selected trip?"), Text,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        await RunDatabaseAsync(async () => { await trips!.DeleteAsync(trip.MaChuyen); await BindRowsAsync(); });
    }

    private void ClearWeather()
    {
        weatherCancellation?.Cancel();
        weatherCancellation = null;
        forecast = null; weatherState = "idle";
        progress.Style = ProgressBarStyle.Blocks; progress.Value = 0;
        RenderWeather(); UpdateButtons();
    }

    private async Task FetchWeatherAsync()
    {
        if (string.IsNullOrWhiteSpace(txtPlace.Text)) { ShowError("place"); return; }
        weatherCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        weatherCancellation = cancellation;
        forecast = null; weatherState = "loading";
        progress.Style = ProgressBarStyle.Marquee; progress.MarqueeAnimationSpeed = 30;
        RenderWeather(); UpdateButtons();
        try
        {
            var result = await weatherService.GetAsync(txtPlace.Text.Trim(), DateOnly.FromDateTime(dtpDate.Value), cancellation.Token);
            if (closing || weatherCancellation != cancellation) return;
            forecast = result; weatherState = result is null ? "noData" : "ready";
        }
        catch (OperationCanceledException)
        {
            if (!closing && weatherCancellation == cancellation) weatherState = "network";
        }
        catch (PlaceNotFoundException)
        {
            if (!closing && weatherCancellation == cancellation) weatherState = "notFound";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            if (!closing && weatherCancellation == cancellation) weatherState = "network";
        }
        finally
        {
            if (!closing && weatherCancellation == cancellation)
            {
                weatherCancellation = null;
                progress.Style = ProgressBarStyle.Blocks; progress.Value = 0;
                RenderWeather(); UpdateButtons();
            }
        }
    }

    private void UpdateButtons()
    {
        if (closing) return;
        grpTrip.Enabled = !databaseBusy;
        btnAdd.Enabled = databaseReady; btnEdit.Enabled = btnDelete.Enabled = databaseReady && Selected is not null;
        btnReset.Enabled = true;
        txtSearch.Enabled = chkPending.Enabled = dtpFrom.Enabled = dtpTo.Enabled = btnSearch.Enabled = databaseReady && !databaseBusy;
        grid.Enabled = !databaseBusy;
        btnWeather.Enabled = weatherState != "loading" && !databaseBusy;
    }

    private void UpdateStatus() => lblStatus.Text = status switch
    {
        "starting" => T("Đang khởi động...", "Starting..."),
        "loading" => T("Đang truy cập cơ sở dữ liệu...", "Accessing database..."),
        "databaseError" => T("Không truy cập được CSDL. Kiểm tra cấu hình rồi bấm Làm mới để thử lại.", "Database unavailable. Check configuration and click Reset to retry."),
        _ => T($"{rowCount} chuyến đi • Dữ liệu được lưu trong SQL Server", $"{rowCount} trips • Data saved in SQL Server")
    };

    private void ShowError(string code)
    {
        string message = code switch
        {
            "required" => T("Vui lòng nhập tên chuyến và địa điểm.", "Please enter a trip name and destination."),
            "place" => T("Vui lòng nhập địa điểm.", "Please enter a destination."),
            "past" => T("Ngày đi không được trước hôm nay khi thêm mới.", "New trips cannot depart before today."),
            "numbers" => T("Số người phải từ 1 trở lên; ngân sách phải không âm và tối đa 18 chữ số.", "People must be at least 1; budget must be nonnegative with at most 18 digits."),
            "length" => T("Tên chuyến và địa điểm tối đa 100 ký tự; ghi chú tối đa 200 ký tự.", "Name and destination: max 100 characters; notes: max 200 characters."),
            "transport" => T("Vui lòng chọn phương tiện.", "Please select a transport."),
            "range" => T("Ngày Từ phải nhỏ hơn hoặc bằng ngày Đến.", "The From date must be on or before the To date."),
            "select" => T("Vui lòng chọn một chuyến đi.", "Please select a trip."),
            "settings" => T("Không lưu được ngôn ngữ. Kiểm tra quyền ghi thư mục LocalAppData.", "Cannot save the language. Check write access to LocalAppData."),
            "concurrency" => T("Chuyến đi đã bị xóa ở nơi khác. Bấm Tìm để tải lại.", "The trip was deleted elsewhere. Click Search to reload."),
            _ => T("Không thể truy cập SQL Server. Kiểm tra dịch vụ SQL Server, quyền truy cập và ConnectionStrings trong appsettings.json, sau đó bấm Làm mới.", "Cannot access SQL Server. Check the SQL Server service, permissions and ConnectionStrings in appsettings.json, then click Reset.")
        };
        MessageBox.Show(this, message, T("Thông báo", "Notice"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ApplyLanguage()
    {
        var culture = CultureInfo.GetCultureInfo(English ? "en-US" : "vi-VN");
        CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = culture;
        Text = lblTitle.Text = T("Kế hoạch dã ngoại cuối tuần", "Weekend picnic planner");
        lblLanguage.Text = T("Ngôn ngữ", "Language"); grpTrip.Text = T("Thông tin chuyến đi", "Trip information");
        string[] vi = ["Tên chuyến", "Địa điểm", "Ngày đi", "Số người", "Ngân sách", "Phương tiện"];
        string[] en = ["Trip name", "Destination", "Date", "People", "Budget ₫", "Transport"];
        for (int i = 0; i < 6; i++) fieldLabels[i].Text = English ? en[i] : vi[i];
        btnAdd.Text = T("Thêm", "Add"); btnEdit.Text = T("Sửa", "Edit");
        btnDelete.Text = T("Xóa", "Delete"); btnReset.Text = T("Làm mới", "Reset");
        btnSearch.Text = T("Tìm", "Search"); btnWeather.Text = T("Xem thời tiết", "Weather");
        chkDone.Text = T("Đã hoàn thành", "Completed"); lblNotes.Text = T("Ghi chú", "Notes");
        lblFrom.Text = T("Từ", "From"); lblTo.Text = T("Đến", "To");
        chkPending.Text = T("Chỉ hiện chuyến chưa hoàn thành", "Show unfinished trips only");
        txtSearch.PlaceholderText = T("Tìm tên chuyến / địa điểm...", "Search trip / destination...");
        int transport = cboTransport.SelectedIndex;
        cboTransport.Items.Clear();
        cboTransport.Items.AddRange(English ? ["Motorbike", "Car", "Coach", "Train"] : TripService.Transports);
        cboTransport.SelectedIndex = transport;
        string[] headers = English ? ["ID", "Trip name", "Destination", "Date", "People", "Budget (VND)", "Transport", "Completed"] : ["Mã", "Tên chuyến", "Địa điểm", "Ngày đi", "Số người", "Ngân sách", "Phương tiện", "Hoàn thành"];
        for (int i = 0; i < headers.Length; i++) grid.Columns[i].HeaderText = headers[i];
        dtpDate.CustomFormat = dtpFrom.CustomFormat = dtpTo.CustomFormat = English ? "MM/dd/yyyy" : "dd/MM/yyyy";
        grid.Columns[3].DefaultCellStyle.Format = dtpDate.CustomFormat;
        grid.Columns[3].DefaultCellStyle.FormatProvider = culture;
        grid.Columns[5].DefaultCellStyle.FormatProvider = culture;
        // Force NumericUpDown to reformat its cached edit text using the new CultureInfo.
        nudBudget.ThousandsSeparator = false; nudBudget.ThousandsSeparator = true;
        grid.Invalidate(); RenderWeather(); UpdateStatus(); UpdateButtons();
    }

    private void FormatTransport(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (!English || e.ColumnIndex != 6 || e.Value is not string value) return;
        e.Value = value switch { "Xe máy" => "Motorbike", "Ô tô" => "Car", "Xe khách" => "Coach", "Tàu hỏa" => "Train", _ => value };
        e.FormattingApplied = true;
    }

    private void RenderWeather()
    {
        lblForecast.Text = T("Dự báo ngày ", "Forecast for ") + dtpDate.Value.ToString(dtpDate.CustomFormat) + " - " + txtPlace.Text;
        pnlWarning.Visible = false;
        pnlWeather.BackColor = SystemColors.Control;
        lblTemperature.Text = lblRain.Text = "";
        lblDescription.Text = weatherState switch
        {
            "loading" => T("Đang tải...", "Loading..."),
            "noData" => T("Chưa có dữ liệu", "No forecast data"),
            "notFound" => T("Không thấy địa danh", "Place not found"),
            "network" => T("Lỗi mạng / quá hạn", "Network / timeout"),
            "ready" => WeatherDescription(forecast!.Code),
            _ => T("Chưa tra thời tiết", "Weather not loaded")
        };
        if (weatherState == "idle") lblTemperature.Text = T("Bấm Xem thời tiết để tra cứu.", "Click Weather to look up.");
        if (weatherState == "noData")
        {
            lblTemperature.Text = T("Chưa có dữ liệu dự báo.", "No forecast data available.");
            lblRain.Text = T("Dự báo tối đa 16 ngày.", "Up to 16 days ahead.");
        }
        if (weatherState == "notFound")
        {
            lblTemperature.Text = T("Thử tên không dấu", "Try an unaccented name");
            lblRain.Text = T("Ví dụ: Da Lat", "For example: Da Lat");
        }
        if (weatherState == "network") lblTemperature.Text = T("Kiểm tra mạng rồi thử lại.", "Check connection and retry.");
        if (forecast is { } result)
        {
            lblTemperature.Text = T("Nhiệt độ: ", "Temperature: ") + $"{result.Minimum:0.#} - {result.Maximum:0.#}°C";
            lblRain.Text = T("Khả năng mưa: ", "Rain probability: ") + (result.Rain is null ? T("Chưa rõ", "Unknown") : $"{result.Rain}%");
            pnlWarning.Visible = result.Rain >= 60;
            if (result.Rain >= 60) pnlWeather.BackColor = Color.FromArgb(255, 240, 217);
        }
        lblWarning.Text = T("Nên mang áo mưa hoặc cân nhắc dời lịch", "Bring a raincoat or consider rescheduling");
        picWeather.Invalidate();
    }

    private string WeatherDescription(int code) => WeatherService.Kind(code) switch
    {
        "clear" => T("Trời quang", "Clear sky"), "cloud" => T("Có mây", "Cloudy"),
        "fog" => T("Sương mù", "Fog"), "rain" => T("Mưa", "Rain"),
        "snow" => T("Tuyết", "Snow"), "storm" => T("Dông", "Thunderstorm"), _ => T("Không xác định", "Unknown")
    };

    private void PaintWeather(object? sender, PaintEventArgs e)
    {
        if (forecast is null) return;
        string kind = WeatherService.Kind(forecast.Code);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(25, 30, 28), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        if (kind == "clear")
        {
            using var sunPen = new Pen(Color.DarkOrange, 3);
            e.Graphics.FillEllipse(Brushes.Gold, 24, 22, 30, 30);
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4;
                e.Graphics.DrawLine(sunPen, 39 + (float)Math.Cos(angle) * 21, 37 + (float)Math.Sin(angle) * 21, 39 + (float)Math.Cos(angle) * 29, 37 + (float)Math.Sin(angle) * 29);
            }
            return;
        }
        using var cloud = new GraphicsPath();
        cloud.AddBezier(19, 40, 2, 40, 0, 18, 20, 16);
        cloud.AddBezier(20, 16, 24, 0, 49, 0, 54, 18);
        cloud.AddBezier(54, 18, 76, 18, 77, 40, 58, 40);
        cloud.AddLine(58, 40, 19, 40);
        e.Graphics.FillPath(Brushes.WhiteSmoke, cloud); e.Graphics.DrawPath(pen, cloud);
        if (kind == "rain") for (int i = 0; i < 3; i++) e.Graphics.DrawLine(pen, 24 + i * 14, 48, 20 + i * 14, 61 + (i == 1 ? 5 : 0));
        if (kind == "fog") for (int i = 0; i < 3; i++) e.Graphics.DrawLine(pen, 12, 48 + i * 8, 65, 48 + i * 8);
        if (kind == "snow") for (int i = 0; i < 3; i++)
        {
            int x = 21 + i * 17;
            e.Graphics.DrawLine(pen, x - 4, 53, x + 4, 61); e.Graphics.DrawLine(pen, x - 4, 61, x + 4, 53);
        }
        if (kind == "storm")
        {
            Point[] lightning = [new(38, 42), new(25, 58), new(37, 58), new(30, 71), new(54, 51), new(42, 51)];
            e.Graphics.FillPolygon(Brushes.DarkOrange, lightning);
        }
    }
}
