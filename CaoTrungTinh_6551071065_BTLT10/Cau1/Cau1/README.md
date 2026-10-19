# Câu 1 – Sổ chi tiêu cá nhân

Đây là dự án **Cau1 của BTLT10**. Giao diện nằm trong `Form1.Designer.cs`, xử lý trong `Form1.cs`, khởi động tại `Program.cs`. Constructor không tham số chỉ tạo giao diện để mở được Designer, không truy cập SQL Server. Khi chạy, Program truyền cấu hình EF vào constructor có tham số để tải dữ liệu và nối các sự kiện.

## Chạy bằng Visual Studio

1. Dừng Debug bằng **Shift+F5** nếu đang chạy bản Form1 cũ.
2. Nếu Visual Studio hỏi reload file/project đã thay đổi bên ngoài, chọn **Reload**.
3. Đóng tab `Form1.cs [Design]` cũ, mở lại `Form1.cs` → **View Designer** (Shift+F7).
4. **Build → Rebuild Solution**, rồi F5. Chọn **Cau1** của BTLT10 làm Startup Project nếu solution có nhiều dự án.

Đang dùng .NET 10 Windows Forms và SQL Server `.` / database `QL_ChiTieu`. CSDL đã có 12 khoản mẫu tổng 1.250.000 đ trên máy hiện tại; lọc Ăn uống trong tháng có 4 khoản, tổng 145.000 đ. Tìm bằng nút Tìm hoặc Enter. Tổng tiền tính theo kết quả hiển thị. Sau CRUD giữ bộ lọc hiện tại; Làm mới trở về tháng hiện tại và xóa các ô nhập.

## CSDL / Database First

Trên máy khác, chạy `Database/01_CreateDatabase.sql` bằng SSMS, hoặc:

```powershell
sqlcmd -S . -E -C -f 65001 -b -i Database\01_CreateDatabase.sql
```

Script không xóa bảng/dữ liệu sẵn có và chỉ thêm mẫu khi bảng trống. Model/DbContext chuyển từ bản đã scaffold trên bảng này. Để scaffold lại từ thư mục dự án:

```powershell
dotnet tool restore
dotnet ef dbcontext scaffold "Server=.;Database=QL_ChiTieu;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --context ChiTieuContext --context-dir Data --output-dir Models --namespace Cau1.Models --context-namespace Cau1.Data --table dbo.ChiTieu --no-onconfiguring --force
```

EF ánh xạ thuộc tính `MaCt` sang cột SQL `MaCT`. `--force` ghi đè Model/DbContext; không đặt code xử lý giao diện vào các file sinh tự động.

## Cấu hình / log

Copy `appsettings.example.json` thành `appsettings.json` nếu lấy source từ Git. Sửa `ConnectionStrings:ChiTieu` cho đúng SQL Server; không phải build lại. `appsettings.json` và `appsettings.Development.json` bị bỏ qua bởi .gitignore vì có thể chứa mật khẩu. Bật `DOTNET_ENVIRONMENT=Development` để đọc file Development ghi đè. File Development không được publish. Các file cấu hình dùng Copy if newer.

Log nằm cạnh exe: `Logs/log-yyyyMMdd.txt`. Thêm/Sửa/Xóa ghi Information kèm MaCT; lỗi CSDL ghi Error và báo thông báo thân thiện. Cần đặt ứng dụng trong thư mục có quyền ghi, tránh Program Files. `TrustServerCertificate=True` dùng cho bài tập cục bộ; triển khai thật nên dùng chứng chỉ hợp lệ.

## Đóng gói

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

Chép toàn bộ thư mục `publish` sang laptop đã cài **.NET 10 Desktop Runtime x64**. Tạo CSDL và chỉnh `appsettings.json` đúng máy đích, rồi chạy `Cau1.exe`. Với Windows Authentication, tài khoản Windows máy đích cần có quyền truy cập SQL Server. Chưa xác minh trên laptop thứ hai.
