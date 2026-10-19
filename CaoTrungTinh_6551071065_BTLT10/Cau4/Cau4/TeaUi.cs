using System.Globalization;
using Serilog;

namespace Cau4;

internal static class TeaUi
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("vi-VN");
    public static string Money(decimal value) => value.ToString("N0", Culture) + " đ";
    public static void Setup(Form form, string title, Size size)
    {
        form.Text = title; form.Font = new Font("Segoe UI", 9F); form.ClientSize = size;
        form.MinimumSize = new Size(850, 650); form.StartPosition = FormStartPosition.CenterScreen;
        form.BackColor = SystemColors.Control;
    }
    public static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true,
        MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = SystemColors.Control, RowHeadersWidth = 28,
        BorderStyle = BorderStyle.FixedSingle, EnableHeadersVisualStyles = false,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = SystemColors.Control },
        Margin = new Padding(0, 5, 0, 0)
    };
    public static void Column(DataGridView grid, string name, string title, bool money = false)
    {
        var column = new DataGridViewTextBoxColumn { Name = name, DataPropertyName = name, HeaderText = title };
        if (money) column.DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", FormatProvider = Culture, Alignment = DataGridViewContentAlignment.MiddleRight };
        grid.Columns.Add(column);
    }
    public static Button Button(string text, Func<Task> action, Action? finished = null)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 29, MinimumSize = new Size(85, 29), Margin = new Padding(3) };
        button.Click += async (_, _) =>
        {
            button.Enabled = false;
            try { await action(); }
            catch (Exception ex) { Error(button, ex); }
            finally { if (!button.IsDisposed) { button.Enabled = true; finished?.Invoke(); } }
        };
        return button;
    }
    public static void Error(Control owner, Exception ex)
    {
        // Không ghi connection string hay URL chứa số tài khoản vào log.
        Log.Error("Thao tác thất bại: {ErrorType}", ex.GetType().Name);
        MessageBox.Show(owner, "Không thể hoàn tất thao tác. Kiểm tra kết nối SQL Server, cấu hình và dữ liệu nhập.\n\n" + ex.GetBaseException().Message, "Bán hàng quán trà sữa", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    public static void Field(TableLayoutPanel panel, int row, string title, Control input)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        input.Dock = DockStyle.Fill; input.Margin = new Padding(3, 4, 3, 3); panel.Controls.Add(input, 1, row);
    }
}
