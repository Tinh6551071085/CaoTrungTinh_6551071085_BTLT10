# Quản lý nhà trọ — Câu 2

Ứng dụng Windows Forms .NET 10, SQL Server, EF Core Database First và Serilog.

## Chạy ứng dụng

1. Mở `Cau2.slnx` bằng Visual Studio có .NET 10 và workload .NET desktop development.
2. CSDL `QL_NhaTro` đã được tạo trên SQL Server mặc định của máy này, có 6 phòng và 3 khách mẫu.
3. Nếu chạy trên máy khác: chạy `database/QL_NhaTro.sql` bằng SSMS hoặc `sqlcmd -S . -E -C -b -f 65001 -i database/QL_NhaTro.sql`.
4. Sửa `ConnectionString` trong `Cau2/appsettings.json` nếu tên server khác. Với LocalDB, dùng `Server=(localdb)\\MSSQLLocalDB` trong JSON.
5. Nhấn F5. Nếu CSDL tạm mất kết nối, bấm Làm mới sau khi kết nối lại.

Không tự xóa hoặc tạo lại bảng khi mở ứng dụng. Script SQL chỉ thêm dữ liệu mẫu khi bảng phòng chưa có dữ liệu.

## Chức năng

- Thêm: bấm Làm mới, nhập thông tin và chọn phòng trống, sau đó bấm Thêm.
- Sửa/Xóa: bấm vào dòng khách trên bảng. Sửa cho phép giữ phòng hiện tại hoặc chuyển sang phòng trống. Xóa có hộp thoại xác nhận.
- Phòng chuyển sang Đang thuê khi thêm khách. Khi chuyển/xóa người cuối cùng, phòng cũ chuyển Trống. Các cập nhật nằm trong giao dịch Serializable.
- Tìm họ tên/SĐT bằng nút Tìm hoặc Enter; kết hợp lọc phòng, tỉnh; Bỏ lọc để xem tất cả.
- Tỉnh/thành và phường/xã tải từ API v2. Mỗi request có timeout 10 giây. Đổi tỉnh hủy request phường/xã trước đó.
- API lỗi: có thể nhập địa chỉ thủ công. Địa chỉ nhập tay chưa biết mã được lưu mã 0. Tên vẫn hiển thị đầy đủ.
- Ctrl+Shift+F12 hiện/ẩn nút Gây lỗi thử. Bấm nút này để thử xử lý lỗi luồng UI.
- Log theo ngày: `%LOCALAPPDATA%\Cau2\Logs\nhatro-YYYYMMDD.log`, giữ 14 file. Có exception và stack trace.
- Lỗi UI được bắt và ứng dụng tiếp tục chạy. Lỗi nghiêm trọng ngoài UI có thể buộc runtime kết thúc; handler ghi log và thông báo trước khi kết thúc.

## Database First

Model trong `Cau2/Models` và context `Cau2/Data/QlNhaTroContext.cs` được sinh từ CSDL bằng công cụ EF Core 10.0.12:

```powershell
.\.tools\dotnet-ef dbcontext scaffold 'Server=.;Database=QL_NhaTro;Trusted_Connection=True;TrustServerCertificate=True' Microsoft.EntityFrameworkCore.SqlServer --project Cau2 --output-dir Models --context-dir Data --context QlNhaTroContext --no-onconfiguring --no-pluralize
```

Nếu chưa có công cụ: `dotnet tool install dotnet-ef --tool-path .tools --version 10.0.12`.
Context nhận cấu hình từ `Database.Open()`, chuỗi kết nối không được ghi trong model sinh tự động. Hai navigation property: `KhachThue.MaPhongNavigation` và `PhongTro.KhachThue`.

Tham khảo: [EF Core reverse engineering](https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/), [API hành chính v2](https://provinces.open-api.vn/api/v2/).

## Các tình huống kiểm tra thủ công

- Mở form: dữ liệu SQL hiện, tải danh sách tỉnh; chọn Hà Nội để tải phường/xã.
- Thêm khách vào P04, sửa sang P05, kiểm tra trạng thái phòng cũ/mới; xóa khách vừa thêm và kiểm tra phòng trống.
- Thử tên rỗng, SĐT không hợp lệ, thiếu địa chỉ, thao tác sửa/xóa khi chưa chọn khách.
- Tìm theo tên/SĐT, kết hợp lọc phòng/tỉnh và Bỏ lọc.
- Ngắt mạng, mở lại form, nhập tay tỉnh/phường và lưu; mở lại khách, kiểm tra địa chỉ còn đúng.
- Đổi tỉnh nhanh, xác nhận danh sách phường thuộc tỉnh cuối cùng.
- Ctrl+Shift+F12, bấm Gây lỗi thử: hộp thoại thân thiện, form tiếp tục dùng được, log có stack trace.

Build đã được kiểm tra riêng; các tình huống tương tác trên cần thực hiện trong phiên chạy ứng dụng.
