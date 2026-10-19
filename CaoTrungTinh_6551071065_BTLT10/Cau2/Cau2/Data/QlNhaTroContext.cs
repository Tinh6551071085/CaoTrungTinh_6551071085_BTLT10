using System;
using System.Collections.Generic;
using Cau2.Models;
using Microsoft.EntityFrameworkCore;

namespace Cau2.Data;

public partial class QlNhaTroContext : DbContext
{
    public QlNhaTroContext(DbContextOptions<QlNhaTroContext> options)
        : base(options)
    {
    }

    public virtual DbSet<KhachThue> KhachThue { get; set; }

    public virtual DbSet<PhongTro> PhongTro { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KhachThue>(entity =>
        {
            entity.HasKey(e => e.MaKhach).HasName("PK__KhachThu__D0CB8DDDEC93D225");

            entity.Property(e => e.GioiTinh).HasMaxLength(5);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TenPhuongXa).HasMaxLength(100);
            entity.Property(e => e.TenTinh).HasMaxLength(100);

            entity.HasOne(d => d.MaPhongNavigation).WithMany(p => p.KhachThue)
                .HasForeignKey(d => d.MaPhong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KhachThue_PhongTro");
        });

        modelBuilder.Entity<PhongTro>(entity =>
        {
            entity.HasKey(e => e.MaPhong).HasName("PK__PhongTro__20BD5E5B68A10A76");

            entity.HasIndex(e => e.TenPhong, "UQ__PhongTro__AE382B29DCC38236").IsUnique();

            entity.Property(e => e.GiaThue).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TenPhong).HasMaxLength(20);
            entity.Property(e => e.TinhTrang)
                .HasMaxLength(20)
                .HasDefaultValue("Trống");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
