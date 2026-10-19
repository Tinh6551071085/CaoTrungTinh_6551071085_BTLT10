-- UTF-8. Với sqlcmd dùng -f 65001 để giữ tiếng Việt.
USE master;
GO
IF DB_ID(N'QL_ChiTieu') IS NULL CREATE DATABASE QL_ChiTieu;
GO
USE QL_ChiTieu;
GO
IF OBJECT_ID(N'dbo.ChiTieu', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChiTieu (
        MaCT int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChiTieu PRIMARY KEY,
        NgayChi date NOT NULL,
        NoiDung nvarchar(100) NOT NULL,
        LoaiChi nvarchar(30) NOT NULL,
        SoTien decimal(18,0) NOT NULL CONSTRAINT CK_ChiTieu_SoTien CHECK (SoTien > 0),
        GhiChu nvarchar(200) NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.ChiTieu)
BEGIN
    DECLARE @DauThang date = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
    INSERT dbo.ChiTieu (NgayChi, NoiDung, LoaiChi, SoTien, GhiChu)
    SELECT DATEADD(day, CASE WHEN n < DAY(GETDATE()) THEN n ELSE DAY(GETDATE()) - 1 END, @DauThang),
        NoiDung, LoaiChi, SoTien, GhiChu
    FROM (VALUES
        (0, N'Cơm trưa', N'Ăn uống', 35000, NULL),
        (1, N'Xăng xe', N'Đi lại', 60000, NULL),
        (2, N'Photo giáo trình', N'Học tập', 25000, NULL),
        (3, N'Xem phim', N'Giải trí', 90000, NULL),
        (4, N'Trà sữa', N'Ăn uống', 40000, NULL),
        (5, N'Mua sắm', N'Khác', 150000, N'Đồ dùng cá nhân'),
        (6, N'Tiền điện', N'Khác', 500000, NULL),
        (7, N'Ăn sáng', N'Ăn uống', 25000, NULL),
        (8, N'Mua sách', N'Học tập', 180000, NULL),
        (9, N'Cà phê với bạn', N'Ăn uống', 45000, NULL),
        (10, N'Vé xe buýt', N'Đi lại', 20000, NULL),
        (11, N'Cầu lông', N'Giải trí', 80000, NULL)
    ) AS Mau(n, NoiDung, LoaiChi, SoTien, GhiChu);
END;
GO
