-- Chạy bằng sqlcmd -f 65001. Chỉ sửa đúng dữ liệu mẫu lỗi đã được đối chiếu.
USE QL_TraSua;
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- Không thay constraint nếu đang có dữ liệu thanh toán ngoài hai giá trị hợp lệ.
    IF EXISTS (SELECT 1 FROM dbo.HoaDon WHERE HinhThucTT NOT IN (N'Tiền mặt', N'Chuyển khoản'))
        THROW 50001, N'Có hóa đơn với hình thức thanh toán cần kiểm tra riêng. Chưa sửa dữ liệu.', 1;

    DECLARE @Categories TABLE (Id int, OldName varbinary(200), NewName nvarchar(50));
    INSERT @Categories VALUES
    (1,0x54007200C300A00020007300E100BB00AF006100,N'Trà sữa'),
    (2,0x54007200C300A000200074007200C300A100690020006300C300A2007900,N'Trà trái cây'),
    (4,0xC4009000E100BB001C202000C40092016E0020007600E100BA00B7007400,N'Đồ ăn vặt');
    UPDATE target SET TenDM = source.NewName
    FROM dbo.DanhMucMon AS target JOIN @Categories AS source ON target.MaDM = source.Id
    WHERE CONVERT(varbinary(200), target.TenDM) = source.OldName;
    DECLARE @CategoryCount int = @@ROWCOUNT;

    DECLARE @Items TABLE (Id int, OldName varbinary(400), NewName nvarchar(100));
    INSERT @Items VALUES
    (1,0x54007200C300A00020007300E100BB00AF00610020007400720075007900E100BB0081006E00200074006800E100BB0018206E006700,N'Trà sữa truyền thống'),
    (2,0x54007200C300A00020007300E100BB00AF006100200074007200C300A2006E00200063006800C300A2007500,N'Trà sữa trân châu'),
    (3,0x54007200C300A00020007300E100BB00AF00610020006D0061007400630068006100,N'Trà sữa matcha'),
    (4,0x54007200C300A0002000C4001820C300A0006F002000630061006D0020007300E100BA00A300,N'Trà đào cam sả'),
    (5,0x54007200C300A00020007600E100BA00A3006900,N'Trà vải'),
    (6,0x54007200C300A00020006300680061006E00680020006400C300A2007900,N'Trà chanh dây'),
    (7,0x54007200C300A2006E00200063006800C300A20075002000C400182065006E00,N'Trân châu đen'),
    (8,0x54006800E100BA00A10063006800200074007200C300A100690020006300C300A2007900,N'Thạch trái cây'),
    (9,0x500075006400640069006E006700200074007200E100BB00A9006E006700,N'Pudding trứng'),
    (10,0x4B0068006F006100690020007400C300A20079002000630068006900C300AA006E00,N'Khoai tây chiên'),
    (11,0x50006800C300B40020006D00610069002000710075006500,N'Phô mai que'),
    (12,0x4200C300A1006E006800200066006C0061006E00,N'Bánh flan');
    UPDATE target SET TenMon = source.NewName
    FROM dbo.MonUong AS target JOIN @Items AS source ON target.MaMon = source.Id
    WHERE CONVERT(varbinary(400), target.TenMon) = source.OldName;
    DECLARE @ItemCount int = @@ROWCOUNT;

    -- Chỉ thay constraint lỗi đã xác định hoặc constraint chuẩn do script này tạo.
    DECLARE @ConstraintName sysname;
    SELECT @ConstraintName = name FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.HoaDon')
      AND name IN (N'CK__HoaDon__HinhThuc__5441852A', N'CK_HoaDon_HinhThucTT');
    IF @ConstraintName IS NULL
        THROW 50002, N'Không tìm thấy constraint đã xác định; dừng để tránh thay schema ngoài phạm vi.', 1;
    DECLARE @DropConstraint nvarchar(300) = N'ALTER TABLE dbo.HoaDon DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
    EXEC sys.sp_executesql @DropConstraint;
    ALTER TABLE dbo.HoaDon WITH CHECK ADD CONSTRAINT CK_HoaDon_HinhThucTT
        CHECK (HinhThucTT IN (N'Tiền mặt', N'Chuyển khoản'));

    COMMIT TRANSACTION;
    SELECT @CategoryCount AS CategoriesRepaired, @ItemCount AS ItemsRepaired;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
