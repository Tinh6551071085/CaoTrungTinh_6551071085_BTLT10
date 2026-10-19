IF DB_ID(N'QL_NhaTro') IS NULL CREATE DATABASE QL_NhaTro;
GO
USE QL_NhaTro;
GO
IF OBJECT_ID(N'dbo.PhongTro') IS NULL
CREATE TABLE dbo.PhongTro (
 MaPhong int IDENTITY PRIMARY KEY,
 TenPhong nvarchar(20) NOT NULL UNIQUE,
 GiaThue decimal(18,0) NOT NULL CHECK(GiaThue >= 0),
 TinhTrang nvarchar(20) NOT NULL DEFAULT N'Trống' CHECK(TinhTrang IN(N'Trống',N'Đang thuê'))
);
IF OBJECT_ID(N'dbo.KhachThue') IS NULL
CREATE TABLE dbo.KhachThue (
 MaKhach int IDENTITY PRIMARY KEY,
 HoTen nvarchar(100) NOT NULL,
 GioiTinh nvarchar(5) NOT NULL,
 NgaySinh date NOT NULL,
 SoDienThoai varchar(15) NOT NULL,
 MaTinh int NOT NULL,
 TenTinh nvarchar(100) NOT NULL,
 MaPhuongXa int NOT NULL,
 TenPhuongXa nvarchar(100) NOT NULL,
 DaDangKyTamTru bit NOT NULL,
 MaPhong int NOT NULL CONSTRAINT FK_KhachThue_PhongTro REFERENCES dbo.PhongTro(MaPhong)
);
IF NOT EXISTS(SELECT 1 FROM dbo.PhongTro)
BEGIN
 INSERT dbo.PhongTro(TenPhong,GiaThue,TinhTrang) VALUES
 (N'P01',1500000,N'Đang thuê'),(N'P02',1600000,N'Đang thuê'),
 (N'P03',1800000,N'Đang thuê'),(N'P04',2000000,N'Trống'),
 (N'P05',1700000,N'Trống'),(N'P06',1900000,N'Trống');
 INSERT dbo.KhachThue(HoTen,GioiTinh,NgaySinh,SoDienThoai,MaTinh,TenTinh,MaPhuongXa,TenPhuongXa,DaDangKyTamTru,MaPhong)
 SELECT N'Nguyễn Văn An',N'Nam','20040512','0912345678',1,N'Thành phố Hà Nội',0,N'Phường Ba Đình',1,MaPhong FROM dbo.PhongTro WHERE TenPhong=N'P03';
 INSERT dbo.KhachThue(HoTen,GioiTinh,NgaySinh,SoDienThoai,MaTinh,TenTinh,MaPhuongXa,TenPhuongXa,DaDangKyTamTru,MaPhong)
 SELECT N'Trần Thị Bình',N'Nữ','20030120','0876543210',37,N'Tỉnh Ninh Bình',0,N'Phường Nam Định',0,MaPhong FROM dbo.PhongTro WHERE TenPhong=N'P01';
 INSERT dbo.KhachThue(HoTen,GioiTinh,NgaySinh,SoDienThoai,MaTinh,TenTinh,MaPhuongXa,TenPhuongXa,DaDangKyTamTru,MaPhong)
 SELECT N'Lê Văn Cường',N'Nam','20020618','0901234567',38,N'Tỉnh Thanh Hóa',0,N'Phường Hạc Thành',0,MaPhong FROM dbo.PhongTro WHERE TenPhong=N'P02';
END;
GO
