-- Chạy trên SQL Server bằng SSMS hoặc sqlcmd -S . -E -C -f 65001 -i Database\QL_DaNgoai.sql
-- Không xóa hoặc ghi đè dữ liệu đã có. Chỉ nhập 8 dòng khi tạo bảng lần đầu.
IF DB_ID(N'QL_DaNgoai') IS NULL
    CREATE DATABASE [QL_DaNgoai];
GO
USE [QL_DaNgoai];
GO
DECLARE @Created BIT = 0;
IF OBJECT_ID(N'dbo.ChuyenDi', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChuyenDi
    (
        MaChuyen INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChuyenDi PRIMARY KEY,
        TenChuyen NVARCHAR(100) NOT NULL,
        DiaDiem NVARCHAR(100) NOT NULL,
        NgayDi DATE NOT NULL,
        SoNguoi INT NOT NULL CONSTRAINT CK_ChuyenDi_SoNguoi CHECK (SoNguoi >= 1),
        NganSach DECIMAL(18,0) NOT NULL CONSTRAINT CK_ChuyenDi_NganSach CHECK (NganSach >= 0),
        PhuongTien NVARCHAR(30) NOT NULL,
        DaHoanThanh BIT NOT NULL CONSTRAINT DF_ChuyenDi_DaHoanThanh DEFAULT (0),
        GhiChu NVARCHAR(200) NULL
    );
    SET @Created = 1;
END;
IF @Created = 1
BEGIN
    DECLARE @Today DATE = CONVERT(DATE, GETDATE());
    INSERT dbo.ChuyenDi (TenChuyen, DiaDiem, NgayDi, SoNguoi, NganSach, PhuongTien, DaHoanThanh, GhiChu)
    VALUES
    (N'Cắm trại đồi chè', N'Đà Lạt', DATEADD(DAY, 3, @Today), 6, 3500000, N'Ô tô', 0, N'Mang theo lều'),
    (N'Săn mây đầu tuần', N'Sa Pa', DATEADD(DAY, 5, @Today), 4, 5000000, N'Tàu hỏa', 0, NULL),
    (N'Dã ngoại bãi biển', N'Vũng Tàu', DATEADD(DAY, 7, @Today), 8, 2800000, N'Xe khách', 0, NULL),
    (N'Khám phá phố cổ', N'Hội An', DATEADD(DAY, 9, @Today), 5, 4200000, N'Ô tô', 0, NULL),
    (N'Picnic ven hồ', N'Biên Hòa', DATEADD(DAY, 11, @Today), 4, 1500000, N'Xe máy', 1, NULL),
    (N'Trekking cuối tuần', N'Đà Lạt', DATEADD(DAY, 14, @Today), 6, 3800000, N'Xe khách', 0, NULL),
    (N'Ngắm bình minh', N'Phan Rang', DATEADD(DAY, 18, @Today), 3, 2200000, N'Xe máy', 0, NULL),
    (N'Dã ngoại đảo', N'Nha Trang', DATEADD(DAY, 23, @Today), 7, 6500000, N'Tàu hỏa', 0, NULL);
END;
GO
