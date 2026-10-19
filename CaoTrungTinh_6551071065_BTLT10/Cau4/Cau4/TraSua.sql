IF DB_ID(N'QL_TraSua') IS NULL CREATE DATABASE QL_TraSua;
GO
USE QL_TraSua;
GO
IF OBJECT_ID(N'dbo.DanhMucMon', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DanhMucMon (MaDM int IDENTITY PRIMARY KEY, TenDM nvarchar(50) NOT NULL);
    CREATE TABLE dbo.MonUong (
        MaMon int IDENTITY PRIMARY KEY, TenMon nvarchar(100) NOT NULL,
        GiaBan decimal(18,0) NOT NULL CHECK(GiaBan > 0), DangBan bit NOT NULL DEFAULT 1,
        MaDM int NOT NULL REFERENCES dbo.DanhMucMon(MaDM));
    -- Sequence cấp mã trước khi tạo QR; hóa đơn chỉ được lưu khi thu ngân xác nhận.
    CREATE SEQUENCE dbo.HoaDonNumber AS int START WITH 1 INCREMENT BY 1;
    CREATE TABLE dbo.HoaDon (
        MaHD int NOT NULL CONSTRAINT DF_HoaDon_MaHD DEFAULT (NEXT VALUE FOR dbo.HoaDonNumber) PRIMARY KEY,
        NgayLap datetime NOT NULL, TongTien decimal(18,0) NOT NULL CHECK(TongTien > 0),
        HinhThucTT nvarchar(20) NOT NULL CONSTRAINT CK_HoaDon_HinhThucTT CHECK(HinhThucTT IN (N'Tiền mặt', N'Chuyển khoản')),
        DaThanhToan bit NOT NULL DEFAULT 0);
    CREATE TABLE dbo.ChiTietHoaDon (
        MaHD int NOT NULL REFERENCES dbo.HoaDon(MaHD), MaMon int NOT NULL REFERENCES dbo.MonUong(MaMon),
        SoLy int NOT NULL CHECK(SoLy > 0), DonGia decimal(18,0) NOT NULL CHECK(DonGia > 0),
        CONSTRAINT PK_ChiTietHoaDon PRIMARY KEY (MaHD, MaMon));
    INSERT dbo.DanhMucMon(TenDM) VALUES (N'Trà sữa'), (N'Trà trái cây'), (N'Topping'), (N'Đồ ăn vặt');
    INSERT dbo.MonUong(TenMon, GiaBan, DangBan, MaDM) VALUES
    (N'Trà sữa truyền thống',25000,1,1),(N'Trà sữa trân châu',30000,1,1),(N'Trà sữa matcha',35000,1,1),
    (N'Trà đào cam sả',30000,1,2),(N'Trà vải',28000,1,2),(N'Trà chanh dây',28000,1,2),
    (N'Trân châu đen',5000,1,3),(N'Thạch trái cây',7000,1,3),(N'Pudding trứng',8000,1,3),
    (N'Khoai tây chiên',20000,1,4),(N'Phô mai que',25000,1,4),(N'Bánh flan',12000,1,4);
END;
GO
