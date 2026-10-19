#nullable enable

namespace Cau2
{
    partial class Form1
    {
        private System.ComponentModel.IContainer? components = null;
        private GroupBox customerGroup = null!;
        private TextBox fullNameTextBox = null!;
        private RadioButton maleRadioButton = null!;
        private RadioButton femaleRadioButton = null!;
        private DateTimePicker birthDatePicker = null!;
        private TextBox phoneTextBox = null!;
        private ComboBox roomComboBox = null!;
        private ComboBox provinceComboBox = null!;
        private ComboBox wardComboBox = null!;
        private CheckBox residenceCheckBox = null!;
        private TextBox searchTextBox = null!;
        private ComboBox roomFilterComboBox = null!;
        private Button searchButton = null!;
        private Button addButton = null!;
        private Button editButton = null!;
        private Button deleteButton = null!;
        private Button resetButton = null!;
        private DataGridView customerGrid = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel statusLabel = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            customerGroup = new GroupBox();
            fullNameTextBox = new TextBox();
            maleRadioButton = new RadioButton();
            femaleRadioButton = new RadioButton();
            birthDatePicker = new DateTimePicker();
            phoneTextBox = new TextBox();
            roomComboBox = new ComboBox();
            provinceComboBox = new ComboBox();
            wardComboBox = new ComboBox();
            residenceCheckBox = new CheckBox();
            searchTextBox = new TextBox();
            roomFilterComboBox = new ComboBox();
            searchButton = new Button();
            addButton = new Button();
            editButton = new Button();
            deleteButton = new Button();
            resetButton = new Button();
            customerGrid = new DataGridView();
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            customerGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)customerGrid).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();

            customerGroup.Text = "Thông tin khách thuê";
            customerGroup.SetBounds(12, 10, 302, 336);
            customerGroup.Controls.Add(new Label { Text = "Họ tên", Location = new Point(14, 32), AutoSize = true });
            customerGroup.Controls.Add(new Label { Text = "Ngày sinh", Location = new Point(14, 103), AutoSize = true });
            customerGroup.Controls.Add(new Label { Text = "Số điện thoại", Location = new Point(14, 138), AutoSize = true });
            customerGroup.Controls.Add(new Label { Text = "Phòng", Location = new Point(14, 173), AutoSize = true });
            customerGroup.Controls.Add(new Label { Text = "Tỉnh/Thành", Location = new Point(14, 208), AutoSize = true });
            customerGroup.Controls.Add(new Label { Text = "Phường/Xã", Location = new Point(14, 243), AutoSize = true });
            fullNameTextBox.SetBounds(125, 28, 160, 23);
            maleRadioButton.Text = "Nam";
            maleRadioButton.SetBounds(125, 64, 65, 23);
            maleRadioButton.Checked = true;
            femaleRadioButton.Text = "Nữ";
            femaleRadioButton.SetBounds(194, 64, 65, 23);
            birthDatePicker.SetBounds(125, 99, 160, 23);
            birthDatePicker.Format = DateTimePickerFormat.Custom;
            birthDatePicker.CustomFormat = "dd/MM/yyyy";
            birthDatePicker.Value = new DateTime(2004, 5, 12);
            phoneTextBox.SetBounds(125, 134, 160, 23);
            roomComboBox.SetBounds(125, 169, 160, 23);
            roomComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            provinceComboBox.SetBounds(125, 204, 160, 23);
            provinceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            wardComboBox.SetBounds(125, 239, 160, 23);
            wardComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            wardComboBox.DropDownHeight = 120;
            residenceCheckBox.Text = "Đã đăng ký tạm trú";
            residenceCheckBox.SetBounds(14, 285, 200, 24);
            residenceCheckBox.Checked = true;
            customerGroup.Controls.AddRange(new Control[] { fullNameTextBox, maleRadioButton, femaleRadioButton,
                birthDatePicker, phoneTextBox, roomComboBox, provinceComboBox, wardComboBox, residenceCheckBox });

            addButton.Text = "Thêm";
            addButton.SetBounds(12, 360, 70, 27);
            editButton.Text = "Sửa";
            editButton.SetBounds(89, 360, 70, 27);
            deleteButton.Text = "Xóa";
            deleteButton.SetBounds(166, 360, 70, 27);
            resetButton.Text = "Làm mới";
            resetButton.SetBounds(243, 360, 71, 27);
            searchTextBox.SetBounds(324, 14, 199, 23);
            searchTextBox.PlaceholderText = "Tìm theo tên hoặc SĐT";
            var roomFilterLabel = new Label { Text = "Phòng:", Location = new Point(535, 17), AutoSize = true };
            roomFilterComboBox.SetBounds(581, 14, 126, 23);
            roomFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            searchButton.Text = "Tìm";
            searchButton.SetBounds(715, 12, 72, 27);
            searchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            customerGrid.SetBounds(324, 49, 463, 338);
            customerGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            customerGrid.AllowUserToAddRows = false;
            customerGrid.AllowUserToDeleteRows = false;
            customerGrid.ReadOnly = true;
            customerGrid.MultiSelect = false;
            customerGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            customerGrid.RowHeadersWidth = 23;
            customerGrid.RowTemplate.Height = 23;
            customerGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            customerGrid.BackgroundColor = Color.FromArgb(171, 171, 171);
            customerGrid.BorderStyle = BorderStyle.Fixed3D;
            customerGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Họ tên", FillWeight = 115 });
            customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Phone", HeaderText = "SĐT", FillWeight = 95 });
            customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Room", HeaderText = "Phòng", FillWeight = 65 });
            customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rent", HeaderText = "Giá thuê", FillWeight = 95 });
            customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hometown", HeaderText = "Quê quán", FillWeight = 100 });
            customerGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Residence", HeaderText = "Tạm trú", FillWeight = 75 });
            statusStrip.Items.Add(statusLabel);
            statusStrip.SizingGrip = false;
            statusLabel.Text = "Dữ liệu mẫu giao diện • provinces.open-api.vn";
            statusStrip.Dock = DockStyle.Bottom;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(800, 425);
            MinimumSize = new Size(816, 464);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Quản lý nhà trọ - Khách thuê";
            Controls.AddRange(new Control[] { customerGroup, addButton, editButton, deleteButton, resetButton,
                searchTextBox, roomFilterLabel, roomFilterComboBox, searchButton, customerGrid, statusStrip });
            customerGroup.ResumeLayout(false);
            customerGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)customerGrid).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
