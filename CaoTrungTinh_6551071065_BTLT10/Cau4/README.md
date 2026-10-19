# Câu 4 — Bán hàng quán trà sữa

Ứng dụng C# WinForms .NET 10, SQL Server, EF Core Database First và Serilog. Giao diện theo ảnh tham khảo mới: ba cột Chọn món – Đơn hiện tại – Thanh toán. Cột trái có danh mục, danh sách món, số ly và Thêm vào đơn; cột giữa có giỏ hàng, Xóa dòng và tổng tiền; cột phải có chuyển khoản, tiền mặt, QR, số tiền, nội dung và nút Đã nhận tiền màu xanh. Màn hình chính giữ bố cục gọn như ảnh, không có hàng nút phụ: nhấn F2 để quản lý thực đơn, F3 để xem hóa đơn; hoặc mở menu chuột phải. Esc hủy QR đang chờ. Khi tải QR lỗi, vùng ảnh hiển thị tài khoản và hướng dẫn chuyển tay.

## Chạy

1. Cài .NET 10 SDK, SQL Server; mở `Cau4.slnx` trong Visual Studio hỗ trợ .NET 10.
2. Sửa `Cau4/appsettings.json`: `ConnectionStrings:TraSua` trỏ đến SQL Server của máy. Mặc định dùng `localhost`, Windows Authentication.
3. Chạy F5 hoặc `dotnet run --project Cau4/Cau4.csproj`.
4. Khi mở, ứng dụng thực thi `TraSua.sql` để tạo `QL_TraSua` nếu chưa có, 4 bảng cùng 4 danh mục và 12 món mẫu. Tài khoản SQL cần quyền tạo database ở lần chạy đầu. Nếu không có quyền, nhờ quản trị viên chạy script trước; các lần sau cần quyền đọc/ghi dữ liệu và sử dụng sequence.

SQL đã được chạy trên SQL Server localhost và model trong `Cau4/Models` được scaffold trực tiếp từ database.

Khi tạo CSDL bằng dòng lệnh, chỉ định UTF-8 để không làm hỏng tên tiếng Việt và ràng buộc hình thức thanh toán:

```powershell
sqlcmd -S localhost -E -C -b -f 65001 -i Cau4/TraSua.sql
```

Nếu dùng database từng được nhập sai mã hóa trong lần triển khai đầu, chạy một lần:

```powershell
sqlcmd -S localhost -E -C -b -f 65001 -i Cau4/RepairEncoding.sql
```

Script sửa dùng transaction, chỉ sửa các tên mẫu khớp chính xác dữ liệu lỗi đã xác nhận và thay constraint hình thức thanh toán; không xóa database hay hóa đơn. Nếu có hóa đơn với hình thức không hợp lệ hoặc constraint khác phiên bản đã biết, script dừng và rollback để kiểm tra riêng.

## Các chức năng

- **Quản lý thực đơn**: tên món, danh mục từ database, giá, trạng thái đang bán; thêm/sửa/xóa, tìm tên kết hợp lọc danh mục. Món đã có chi tiết hóa đơn không được xóa; bỏ Đang bán để ngừng bán.
- **Bán hàng**: chọn danh mục, chọn món đang bán, chọn số ly, Thêm vào đơn. Thêm lại cùng món cộng số lượng và giữ đơn giá đã chốt trong giỏ. Xóa dòng cập nhật tổng tiền ngay.
- **Thanh toán chuyển khoản**: cấp mã HD riêng, đọc lại Payment từ cấu hình, hiện tài khoản/số tiền/nội dung `TRASUA HD…`, tải ảnh VietQR async vào PictureBox. Giỏ được khóa cho đến khi hoàn tất hoặc hủy thanh toán. Tải ảnh lỗi thì vẫn giữ thông tin chuyển tay, nút Đã nhận tiền và Tiền mặt.
- **Đã nhận tiền**: chỉ bấm sau khi kiểm tra ngân hàng; xác nhận rồi lưu hóa đơn và toàn bộ chi tiết bằng một `SaveChangesAsync`, dùng transaction mặc định của EF Core.
- **Tiền mặt**: xác nhận tiền đã nhận và lưu hóa đơn, không cần QR.
- Khi lưu lỗi, giữ mã đơn và giỏ; thử lại cùng hình thức. Nếu lần trước đã commit nhưng mất phản hồi, hệ thống đối chiếu hóa đơn cũ để không tạo trùng. Sau lần thử lưu, không hủy hoặc đổi hình thức cho đến khi lưu thành công; tránh tạo thêm đơn khi giao dịch cũ có thể đã được lưu.
- **Danh sách hóa đơn**: lọc theo ngày, hiển thị chi tiết khi chọn đơn; tổng doanh thu chỉ gồm đơn đã thanh toán trong ngày.
- Log Information khi tạo QR/lưu đơn và Error khi tải QR lỗi nằm trong `logs/trasua-YYYYMMDD.log` cạnh file chạy; giữ 14 file. Không ghi số tài khoản đầy đủ hoặc URL QR vào log.

## Cấu hình ngân hàng

`Payment` có `BankId`, `AccountNo`, `AccountName`, `Template`, `ShopName`. **Tài khoản `0000000000` và tên `TAI KHOAN DEMO` là dữ liệu giả.** Để quét và chuyển thật, thay bằng tài khoản chính mình, dùng BankId phù hợp. Không dùng thông tin người khác.

Ứng dụng đọc file cấu hình nằm cạnh executable (`bin/Debug/net10.0-windows/appsettings.json` khi chạy Debug). Sửa file này để lần tạo QR kế tiếp dùng ngay cấu hình mới; sửa file nguồn `Cau4/appsettings.json` để lưu cấu hình cho các lần build tiếp theo. Không cần sửa code. Khi thử lại QR của cùng đơn, kiểm tra lại tài khoản trước khi khách quét.

VietQR chỉ tạo mã, không tự xác nhận tiền vào tài khoản. Chưa thực hiện chuyển khoản thật hoặc xác minh bằng ứng dụng ngân hàng với cấu hình giả.

## Database First và mã hóa đơn

`DanhMucMon.MaDM` và `MonUong.MaMon` dùng IDENTITY. `HoaDon.MaHD` dùng SQL Server SEQUENCE thay cho IDENTITY trong bảng gợi ý: cần biết mã HD trước khi khách quét QR nhưng chỉ lưu hóa đơn khi xác nhận tiền. Sequence đảm bảo cấp mã riêng giữa các phiên; hủy QR có thể bỏ qua một số mã, không tái sử dụng mã đã cấp.

Để scaffold lại sau khi sửa schema, cài công cụ `dotnet-ef` 10.x rồi chạy từ thư mục chứa solution:

```powershell
dotnet ef dbcontext scaffold 'Server=localhost;Database=QL_TraSua;Trusted_Connection=True;TrustServerCertificate=True' Microsoft.EntityFrameworkCore.SqlServer --project Cau4/Cau4.csproj --output-dir Models --context TraSuaContext --namespace Cau4 --no-onconfiguring --force
```

Không hard-code connection string trong context scaffold; `TeaDatabase` truyền options đọc từ cấu hình. Những thuộc tính không nullable trong model được khởi tạo `null!` để phù hợp nullable của dự án.

## Checklist kiểm tra chức năng

1. Khởi động: đủ 4 danh mục, tối thiểu 12 món; mỗi danh mục chỉ hiện món đang bán.
2. Thực đơn: thêm, sửa tên/giá/danh mục, bỏ Đang bán; lọc kết hợp tên và danh mục; xóa món chưa bán, chặn xóa món đã có hóa đơn.
3. Giỏ hàng: thêm 2 ly trà 25.000 rồi thêm lại 1 ly -> một dòng SL 3, tổng 75.000; thêm món khác và xóa dòng, kiểm tra tổng.
4. QR: có số tiền và `TRASUA HD…` đúng giỏ; thông tin chữ khớp cấu hình. Đổi Payment rồi tạo QR mới, kiểm tra tài khoản mới.
5. Mất mạng khi tải QR: thông tin chữ vẫn còn, có thể xác nhận đã nhận tiền chuyển tay hoặc chọn tiền mặt; Error có trong log và không lộ số tài khoản.
6. Lưu tiền mặt và chuyển khoản: mỗi đơn chỉ có một hóa đơn, chi tiết/tổng khớp; lọc ngày và chọn đơn xem chi tiết, doanh thu tăng đúng.
7. Lỗi SQL khi lưu: không mất giỏ hoặc đổi mã đơn; sau khi kết nối lại thử lưu cùng hình thức, không trùng hóa đơn. Không cho thay giỏ trong lúc QR đang chờ.

Build đã thành công. Đã kiểm thử tương tác thêm/sửa/tìm/lọc món, chặn xóa món đã bán, lưu tiền mặt/chuyển khoản, chi tiết/doanh thu, đổi cấu hình, retry lỗi SQL và tải QR lỗi. Chưa chuyển tiền thật hoặc quét bằng ứng dụng ngân hàng với tài khoản thật.
