using Microsoft.EntityFrameworkCore;

namespace Cau4;

public sealed class FormBanHang : Form
{
    private readonly ComboBox categories = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = 999, Value = 1, Width = 75 };
    private readonly ListBox menu = new() { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true };
    private readonly DataGridView basket = TeaUi.Grid();
    private readonly ShoppingCart cart = new();
    private readonly Label total = new() { Text = "Tổng tiền: 0 đ", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
    private readonly PictureBox qr = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
    private readonly TextBox transferInfo = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = SystemColors.Control };
    private readonly Label paymentStatus = new() { Dock = DockStyle.Fill, Text = "Chọn món để bắt đầu.", TextAlign = ContentAlignment.MiddleLeft, ForeColor = SystemColors.GrayText, AutoEllipsis = true };
    private readonly Label paymentAmount = new() { Text = "Số tiền: 0 đ", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label paymentContent = new() { Text = "Nội dung: —", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly ProgressBar progress = new() { Dock = DockStyle.Fill, Visible = false, Style = ProgressBarStyle.Marquee };
    private readonly Button add;
    private readonly Button remove;
    private readonly Button transfer;
    private readonly Button received;
    private readonly Button cash;
    private readonly ToolStripMenuItem cancel;
    private readonly ToolStripMenuItem manageMenu;
    private readonly ToolStripMenuItem invoices;
    private bool ready;
    private bool busy;
    private bool pending;
    private int? invoiceId;
    private string? attemptedMethod;
    private int menuRequest;
    private sealed record MenuRow(int MaMon, string TenMon, decimal GiaBan)
    {
        public string Display => $"{TenMon} - {GiaBan.ToString("N0", TeaUi.Culture)}";
    }

    public FormBanHang()
    {
        TeaUi.Setup(this, "Bán hàng - Quán trà sữa", new Size(740, 380));
        MinimumSize = SizeFromClientSize(new Size(740, 380));
        ClientSize = new Size(740, 380);
        var settings = AppSettings.Load();
        add = TeaUi.Button("Thêm vào đơn", () => RunAsync(AddToCartAsync), UpdateControls);
        remove = TeaUi.Button("Xóa dòng", () => { if (!busy && !pending && basket.CurrentRow?.DataBoundItem is CartItem item) { cart.Remove(item.MaMon); RefreshCart(); } return Task.CompletedTask; }, UpdateControls);
        transfer = TeaUi.Button("Thanh toán chuyển khoản", () => RunAsync(PrepareTransferAsync), UpdateControls);
        received = TeaUi.Button("Đã nhận tiền", () => RunAsync(() => PayAsync("Chuyển khoản")), UpdateControls);
        cash = TeaUi.Button("Tiền mặt", () => RunAsync(() => PayAsync("Tiền mặt")), UpdateControls);
        cancel = new ToolStripMenuItem("Hủy thanh toán", null, (_, _) => CancelPayment()) { ShortcutKeyDisplayString = "Esc" };
        manageMenu = new ToolStripMenuItem("Quản lý thực đơn") { ShortcutKeys = Keys.F2 };
        manageMenu.Click += async (_, _) => await RunAsync(async () => { using var form = new FormThucDon(); form.ShowDialog(this); await LoadCategoriesAsync(); await LoadMenuAsync(); });
        invoices = new ToolStripMenuItem("Danh sách hóa đơn") { ShortcutKeys = Keys.F3 };
        invoices.Click += (_, _) => { using var form = new FormHoaDon(); form.ShowDialog(this); };
        var actions = new ContextMenuStrip(); actions.Items.AddRange(new ToolStripItem[] { manageMenu, invoices, new ToolStripSeparator(), cancel });
        ContextMenuStrip = actions; KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.F2) { manageMenu.PerformClick(); e.Handled = true; } else if (e.KeyCode == Keys.F3) { invoices.PerformClick(); e.Handled = true; } else if (e.KeyCode == Keys.Escape) { cancel.PerformClick(); e.Handled = true; } };

        Text = "Bán hàng - " + settings.Payment.ShopName;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 4), ColumnCount = 1, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        // Ba cột theo ảnh: chọn món, đơn hiện tại, thanh toán.
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        layout.Controls.Add(columns, 0, 0);

        var menuBox = new GroupBox { Text = "Chọn món", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
        var menuLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), RowCount = 5, ColumnCount = 1 };
        menuLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        menuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        menuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        menuLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        menuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        menuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        menuLayout.Controls.Add(new Label { Text = "Danh mục:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        categories.Dock = DockStyle.Fill; categories.Margin = new Padding(0, 0, 0, 8);
        menuLayout.Controls.Add(categories, 0, 1);
        menu.DisplayMember = nameof(MenuRow.Display); menu.Margin = Padding.Empty;
        menu.Font = new Font("Segoe UI", 8F);
        menuLayout.Controls.Add(menu, 0, 2);
        var quantities = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty, Padding = new Padding(0, 22, 0, 8) };
        quantities.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); quantities.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        quantities.Controls.Add(new Label { Text = "Số ly:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        quantity.Dock = DockStyle.Fill; quantity.Margin = new Padding(0, 3, 0, 3); quantities.Controls.Add(quantity, 1, 0);
        menuLayout.Controls.Add(quantities, 0, 3);
        add.Dock = DockStyle.Fill; add.AutoSize = false; add.Margin = new Padding(0, 4, 0, 0); menuLayout.Controls.Add(add, 0, 4);
        menuBox.Controls.Add(menuLayout); columns.Controls.Add(menuBox, 0, 0);

        var orderBox = new GroupBox { Text = "Đơn hiện tại", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
        var orderLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(5), ColumnCount = 1, RowCount = 3 };
        orderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        orderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        TeaUi.Column(basket, "TenMon", "Tên món"); TeaUi.Column(basket, "SoLy", "SL");
        TeaUi.Column(basket, "DonGia", "Đơn giá", true); TeaUi.Column(basket, "ThanhTien", "Thành tiền", true);
        basket.Columns["TenMon"]!.FillWeight = 44; basket.Columns["SoLy"]!.FillWeight = 10;
        basket.Columns["DonGia"]!.FillWeight = 21; basket.Columns["ThanhTien"]!.FillWeight = 25;
        basket.RowHeadersWidth = 20; basket.RowTemplate.Height = 19;
        basket.ColumnHeadersHeight = 22; basket.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        basket.Font = new Font("Segoe UI", 7.5F);
        total.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        basket.BackgroundColor = Color.DarkGray; basket.Margin = Padding.Empty;
        orderLayout.Controls.Add(basket, 0, 0);
        remove.Dock = DockStyle.Fill; remove.AutoSize = false; remove.Margin = new Padding(0, 7, 0, 0); orderLayout.Controls.Add(remove, 0, 1);
        orderLayout.Controls.Add(total, 0, 2);
        orderBox.Controls.Add(orderLayout); columns.Controls.Add(orderBox, 1, 0);

        var paymentBox = new GroupBox { Text = "Thanh toán", Dock = DockStyle.Fill, Margin = Padding.Empty };
        var paymentLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6), ColumnCount = 1, RowCount = 7 };
        paymentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 34, 32, 4 }) paymentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        paymentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (var height in new[] { 25, 25, 30 }) paymentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        // Gán ô tường minh để progress ẩn/hiện không làm dịch ảnh QR.
        transfer.Dock = cash.Dock = received.Dock = DockStyle.Fill;
        transfer.AutoSize = cash.AutoSize = received.AutoSize = false;
        transfer.MinimumSize = cash.MinimumSize = received.MinimumSize = Size.Empty;
        transfer.Font = new Font("Segoe UI", 8F);
        transfer.Margin = new Padding(0, 0, 0, 4); cash.Margin = new Padding(0, 0, 0, 4);
        transfer.Text = "Thanh toán chuyển khoản";
        paymentLayout.Controls.Add(transfer, 0, 0); paymentLayout.Controls.Add(cash, 0, 1); paymentLayout.Controls.Add(progress, 0, 2);
        qr.BorderStyle = BorderStyle.None; qr.BackColor = SystemColors.Control; qr.Margin = new Padding(0, 3, 0, 3);
        var qrArea = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        qrArea.Controls.Add(qr);
        transferInfo.Text = ""; transferInfo.Visible = false;
        transferInfo.ScrollBars = ScrollBars.Vertical; transferInfo.TabStop = false;
        qrArea.Controls.Add(transferInfo); transferInfo.BringToFront();
        paymentLayout.Controls.Add(qrArea, 0, 3);
        paymentAmount.Font = paymentContent.Font = new Font("Segoe UI", 8F);
        paymentLayout.Controls.Add(paymentAmount, 0, 4); paymentLayout.Controls.Add(paymentContent, 0, 5);
        received.BackColor = Color.FromArgb(25, 128, 15); received.ForeColor = Color.White;
        received.FlatStyle = FlatStyle.Flat; received.FlatAppearance.BorderSize = 0; received.Margin = new Padding(0, 3, 0, 2);
        paymentLayout.Controls.Add(received, 0, 6);
        paymentBox.Controls.Add(paymentLayout); columns.Controls.Add(paymentBox, 2, 0);

        var hints = new ToolTip();
        hints.SetToolTip(menu, "F2: Quản lý thực đơn · F3: Danh sách hóa đơn");
        paymentStatus.TextChanged += (_, _) => { hints.SetToolTip(qrArea, paymentStatus.Text); hints.SetToolTip(qr, paymentStatus.Text); };

        categories.SelectedIndexChanged += async (_, _) => { if (!ready) return; try { await LoadMenuAsync(); } catch (Exception ex) { TeaUi.Error(this, ex); } };
        Shown += async (_, _) => await RunAsync(async () => { await TeaDatabase.InitializeAsync(); await LoadCategoriesAsync(); ready = true; await LoadMenuAsync(); paymentStatus.Text = "Chọn món, số ly rồi bấm Thêm vào đơn."; });
        FormClosing += (_, e) =>
        {
            if (busy) { e.Cancel = true; return; }
            if (cart.Items.Count > 0 && MessageBox.Show(this, "Đơn hiện tại chưa lưu. Đóng ứng dụng?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) e.Cancel = true;
        };
        FormClosed += (_, _) => { ReplaceQr(null); actions.Dispose(); hints.Dispose(); };
        UpdateControls();
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true; UpdateControls();
        try { await action(); }
        catch (Exception ex) { TeaUi.Error(this, ex); }
        finally { busy = false; UpdateControls(); }
    }

    private async Task LoadCategoriesAsync()
    {
        var wasReady = ready; ready = false;
        try
        {
            await using var db = TeaDatabase.Open();
            var rows = await db.DanhMucMons.AsNoTracking().OrderBy(x => x.MaDm).ToListAsync();
            categories.DisplayMember = "TenDm"; categories.ValueMember = "MaDm"; categories.DataSource = rows;
        }
        finally { ready = wasReady; }
    }

    private async Task LoadMenuAsync()
    {
        var request = ++menuRequest;
        var categoryId = categories.SelectedValue is int id ? id : 0;
        await using var db = TeaDatabase.Open();
        var rows = await db.MonUongs.AsNoTracking().Where(x => x.DangBan && x.MaDm == categoryId)
            .OrderBy(x => x.MaMon).Select(x => new MenuRow(x.MaMon, x.TenMon, x.GiaBan)).ToListAsync();
        if (!IsDisposed && request == menuRequest) menu.DataSource = rows;
    }

    private async Task AddToCartAsync()
    {
        if (pending || menu.SelectedItem is not MenuRow selected) return;
        await using var db = TeaDatabase.Open();
        var item = await db.MonUongs.AsNoTracking().SingleOrDefaultAsync(x => x.MaMon == selected.MaMon && x.DangBan);
        if (item is null) { MessageBox.Show(this, "Món đã ngừng bán. Hãy chọn món khác."); await LoadMenuAsync(); return; }
        cart.Add(item.MaMon, item.TenMon, (int)quantity.Value, item.GiaBan); RefreshCart();
    }

    private void RefreshCart()
    {
        basket.DataSource = cart.Items.ToList(); total.Text = "Tổng tiền: " + TeaUi.Money(cart.Total);
        paymentAmount.Text = "Số tiền: " + TeaUi.Money(cart.Total); UpdateControls();
    }

    private async Task PrepareTransferAsync()
    {
        if (cart.Items.Count == 0 || attemptedMethod is not null) return;
        // Đọc lại cấu hình mỗi lần tạo QR, không cần sửa code hoặc khởi động lại.
        var settings = AppSettings.Load().Payment; settings.Validate();
        invoiceId ??= await TeaDatabase.ReserveInvoiceNumberAsync(); pending = true;
        ReplaceQr(null);
        transferInfo.Visible = false; qr.Visible = true;
        transferInfo.Text = $"Ngân hàng: {settings.BankId}\r\nSTK: {settings.AccountNo}\r\nChủ TK: {settings.AccountName}";
        paymentAmount.Text = "Số tiền: " + TeaUi.Money(cart.Total);
        paymentContent.Text = "Nội dung: " + PaymentService.TransferContent(invoiceId.Value);
        progress.Visible = true; paymentStatus.Text = "Đang tải mã QR...";
        try
        {
            ReplaceQr(await PaymentService.LoadQrAsync(settings, invoiceId.Value, cart.Total));
            paymentStatus.Text = "Kiểm tra tiền thực nhận tại ngân hàng rồi mới bấm Đã nhận tiền.";
        }
        catch
        {
            paymentStatus.Text = "Không tải được QR. Chuyển tay theo thông tin trên hoặc chọn Tiền mặt.";
            transferInfo.Text = "Không tải được QR.\r\n\r\n" + transferInfo.Text + "\r\n\r\nCó thể chuyển tay hoặc chọn Tiền mặt.";
            transferInfo.Visible = true; qr.Visible = false;
        }
        finally { progress.Visible = false; }
    }

    private async Task PayAsync(string method)
    {
        if (cart.Items.Count == 0 || (method == "Chuyển khoản" && !pending) || (attemptedMethod is not null && attemptedMethod != method)) return;
        var message = method == "Chuyển khoản" ? "Bạn đã kiểm tra ngân hàng và nhận đủ " : "Xác nhận đã nhận tiền mặt ";
        if (MessageBox.Show(this, message + TeaUi.Money(cart.Total) + "?", "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        invoiceId ??= await TeaDatabase.ReserveInvoiceNumberAsync();
        // Giữ nguyên mã, giỏ hàng khi lưu lỗi để thu ngân thử lại an toàn.
        pending = true;
        attemptedMethod = method;
        paymentStatus.Text = "Đang lưu hóa đơn. Nếu kết nối lỗi, bấm lại cùng hình thức để thử lưu lại.";
        var invoice = await InvoiceService.SaveAsync(invoiceId.Value, cart.Items, method);
        cart.Clear(); pending = false; invoiceId = null; attemptedMethod = null; ReplaceQr(null); RefreshCart();
        transferInfo.Text = $"Đã lưu hóa đơn HD{invoice.MaHd}\r\nHình thức: {invoice.HinhThucTt}\r\nTổng tiền: {TeaUi.Money(invoice.TongTien)}";
        paymentAmount.Text = "Số tiền: " + TeaUi.Money(invoice.TongTien);
        paymentContent.Text = "Nội dung: " + PaymentService.TransferContent(invoice.MaHd);
        paymentStatus.Text = "Thanh toán hoàn tất. Có thể lập đơn mới hoặc xem Danh sách hóa đơn.";
        MessageBox.Show(this, $"Đã lưu hóa đơn HD{invoice.MaHd}: {TeaUi.Money(invoice.TongTien)}.", "Thanh toán thành công");
    }

    private void CancelPayment()
    {
        if (busy || !pending || attemptedMethod is not null) return;
        if (MessageBox.Show(this, "Chỉ hủy khi khách chưa chuyển tiền. Hủy mã thanh toán và cho phép sửa giỏ hàng?", "Hủy thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        pending = false; invoiceId = null; ReplaceQr(null); transferInfo.Clear(); paymentContent.Text = "Nội dung: —";
        paymentStatus.Text = "Đã hủy mã thanh toán. Có thể sửa giỏ và tạo QR mới."; UpdateControls();
    }

    private void UpdateControls()
    {
        var editable = ready && !busy && !pending;
        categories.Enabled = menu.Enabled = quantity.Enabled = add.Enabled = manageMenu.Enabled = editable;
        remove.Enabled = editable && cart.Items.Count > 0;
        transfer.Enabled = ready && !busy && cart.Items.Count > 0 && attemptedMethod is null;
        cash.Enabled = ready && !busy && cart.Items.Count > 0 && attemptedMethod is not "Chuyển khoản";
        received.Enabled = ready && !busy && pending && attemptedMethod is not "Tiền mặt";
        cancel.Enabled = !busy && pending && attemptedMethod is null;
        invoices.Enabled = ready && !busy;
    }

    private void ReplaceQr(Image? image)
    {
        var old = qr.Image; qr.Image = image; old?.Dispose();
    }
}
