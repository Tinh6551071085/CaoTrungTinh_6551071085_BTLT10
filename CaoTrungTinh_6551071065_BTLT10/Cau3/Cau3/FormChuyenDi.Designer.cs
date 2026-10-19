#nullable enable

namespace Cau3;

partial class FormChuyenDi
{
    private System.ComponentModel.IContainer? components;
    private Label lblTitle = null!, lblLanguage = null!;
    private GroupBox grpTrip = null!;
    private TextBox txtName = null!, txtPlace = null!, txtSearch = null!;
    private DateTimePicker dtpDate = null!;
    private NumericUpDown nudPeople = null!, nudBudget = null!;
    private ComboBox cboTransport = null!, cboLanguage = null!;
    private CheckBox chkDone = null!, chkPending = null!;
    private Button btnAdd = null!, btnEdit = null!, btnDelete = null!, btnReset = null!, btnWeather = null!, btnSearch = null!;
    private ProgressBar progress = null!;
    private Panel pnlWeather = null!, pnlWarning = null!;
    private Label lblForecast = null!, lblDescription = null!, lblTemperature = null!, lblRain = null!, lblWarning = null!;
    private PictureBox picWeather = null!;
    private DataGridView grid = null!;
    private readonly Label[] fieldLabels = new Label[6];
    private TextBox txtNotes = null!;
    private Label lblNotes = null!, lblFrom = null!, lblTo = null!, lblStatus = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private System.Windows.Forms.Timer searchTimer = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(684, 480);
        MinimumSize = new Size(700, 519);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        Text = "Kế hoạch dã ngoại cuối tuần";

        lblTitle = new Label { Text = Text, Location = new Point(8, 6), AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
        lblLanguage = new Label { Text = "Ngôn ngữ", Location = new Point(526, 9), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        cboLanguage = new ComboBox { Location = new Point(593, 5), Size = new Size(82, 24), DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        cboLanguage.Items.AddRange(["Tiếng Việt", "English"]);
        cboLanguage.SelectedIndex = 0;

        grpTrip = new GroupBox { Text = "Thông tin chuyến đi", Location = new Point(8, 30), Size = new Size(388, 225) };
        string[] captions = ["Tên chuyến", "Địa điểm", "Ngày đi", "Số người", "Ngân sách", "Phương tiện"];
        for (int i = 0; i < captions.Length; i++)
        {
            fieldLabels[i] = new Label { Text = captions[i], Location = new Point(10, 21 + i * 20), Size = new Size(80, 20) };
            grpTrip.Controls.Add(fieldLabels[i]);
        }
        txtName = new TextBox { Location = new Point(91, 18), Size = new Size(287, 23), MaxLength = 100 };
        txtPlace = new TextBox { Location = new Point(91, 38), Size = new Size(287, 23), MaxLength = 100 };
        dtpDate = new DateTimePicker { Location = new Point(91, 58), Size = new Size(112, 23), Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
        nudPeople = new NumericUpDown { Location = new Point(91, 78), Size = new Size(88, 23), Minimum = 1, Maximum = int.MaxValue, Value = 6 };
        nudBudget = new NumericUpDown { Location = new Point(91, 98), Size = new Size(190, 23), Maximum = 999999999999999999m, ThousandsSeparator = true, Increment = 100000, Value = 3500000 };
        cboTransport = new ComboBox { Location = new Point(91, 118), Size = new Size(112, 23), DropDownStyle = ComboBoxStyle.DropDownList };
        cboTransport.Items.AddRange(["Xe máy", "Ô tô", "Xe khách", "Tàu hỏa"]);
        cboTransport.SelectedIndex = 1;
        chkDone = new CheckBox { Text = "Đã hoàn thành", Location = new Point(9, 140), Size = new Size(170, 21) };
        lblNotes = new Label { Text = "Ghi chú", Location = new Point(10, 165), Size = new Size(80, 20) };
        txtNotes = new TextBox { Location = new Point(91, 161), Size = new Size(287, 23), MaxLength = 200 };
        btnAdd = MakeButton("Thêm", 10, 193, 77);
        btnEdit = MakeButton("Sửa", 95, 193, 77);
        btnDelete = MakeButton("Xóa", 180, 193, 77);
        btnReset = MakeButton("Làm mới", 265, 193, 78);
        grpTrip.Controls.AddRange([txtName, txtPlace, dtpDate, nudPeople, nudBudget, cboTransport, chkDone, lblNotes, txtNotes, btnAdd, btnEdit, btnDelete, btnReset]);

        btnWeather = MakeButton("Xem thời tiết", 402, 33, 104);
        progress = new ProgressBar { Location = new Point(512, 38), Size = new Size(163, 13), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        pnlWeather = new Panel { Location = new Point(402, 61), Size = new Size(273, 194), BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        lblForecast = new Label { Location = new Point(5, 4), Size = new Size(261, 28), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Font = new Font("Segoe UI", 8F) };
        picWeather = new PictureBox { Location = new Point(14, 38), Size = new Size(78, 72) };
        picWeather.Paint += PaintWeather;
        lblDescription = new Label { Location = new Point(99, 38), Size = new Size(167, 28), Font = new Font("Segoe UI", 12F) };
        lblTemperature = new Label { Location = new Point(100, 68), Size = new Size(166, 21) };
        lblRain = new Label { Location = new Point(100, 89), Size = new Size(166, 21) };
        pnlWarning = new Panel { Location = new Point(12, 139), Size = new Size(248, 45), BackColor = Color.FromArgb(255, 216, 163), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        var warningIcon = new Label { Text = "❕", ForeColor = Color.DarkOrange, Font = new Font("Segoe UI", 15F, FontStyle.Bold), Location = new Point(3, 1), Size = new Size(25, 30) };
        lblWarning = new Label { Location = new Point(30, 3), Size = new Size(213, 39), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8F), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        pnlWarning.Controls.AddRange([warningIcon, lblWarning]);
        pnlWeather.Controls.AddRange([lblForecast, picWeather, lblDescription, lblTemperature, lblRain, pnlWarning]);

        txtSearch = new TextBox { Location = new Point(8, 265), Size = new Size(185, 23), PlaceholderText = "Tìm tên chuyến / địa điểm..." };
        chkPending = new CheckBox { Text = "Chỉ hiện chuyến chưa hoàn thành", Location = new Point(200, 266), Size = new Size(237, 22) };
        btnSearch = MakeButton("Tìm", 445, 264, 70);
        lblFrom = new Label { Text = "Từ", Location = new Point(8, 296), Size = new Size(42, 20) };
        dtpFrom = new DateTimePicker { Location = new Point(52, 292), Size = new Size(135, 23), Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
        lblTo = new Label { Text = "Đến", Location = new Point(200, 296), Size = new Size(35, 20) };
        dtpTo = new DateTimePicker { Location = new Point(239, 292), Size = new Size(135, 23), Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true, Checked = false };
        lblStatus = new Label { Location = new Point(8, 459), Size = new Size(667, 20), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, AutoEllipsis = true };
        searchTimer = new System.Windows.Forms.Timer(components) { Interval = 300 };
        grid = new DataGridView
        {
            Location = new Point(8, 323), Size = new Size(667, 131),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoGenerateColumns = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Control, BorderStyle = BorderStyle.FixedSingle,
            RowHeadersWidth = 25, RowTemplate = { Height = 20, MinimumHeight = 20 }, ColumnHeadersHeight = 24,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        string[] properties = ["MaChuyen", "TenChuyen", "DiaDiem", "NgayDi", "SoNguoi", "NganSach", "PhuongTien", "DaHoanThanh"];
        string[] headers = ["Mã", "Tên chuyến", "Địa điểm", "Ngày đi", "Số người", "Ngân sách", "Phương tiện", "Hoàn thành"];
        int[] weights = [42, 130, 77, 78, 64, 78, 80, 73];
        for (int i = 0; i < properties.Length; i++)
        {
            DataGridViewColumn column = i == 7 ? new DataGridViewCheckBoxColumn() : new DataGridViewTextBoxColumn();
            column.DataPropertyName = properties[i];
            column.HeaderText = headers[i];
            column.FillWeight = weights[i];
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(column);
        }
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 8F);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F);
        grid.Columns[3].DefaultCellStyle.Format = "dd/MM/yyyy";
        grid.Columns[5].DefaultCellStyle.Format = "N0";

        Controls.AddRange([lblTitle, lblLanguage, cboLanguage, grpTrip, btnWeather, progress, pnlWeather, txtSearch, chkPending, btnSearch, lblFrom, dtpFrom, lblTo, dtpTo, grid, lblStatus]);
        ResumeLayout(false);
        PerformLayout();
    }

    private static Button MakeButton(string text, int x, int y, int width) => new()
    {
        Text = text, Location = new Point(x, y), Size = new Size(width, 23), UseVisualStyleBackColor = true
    };
}
