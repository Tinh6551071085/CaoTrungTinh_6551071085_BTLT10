using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Cau4;

public partial class TraSuaContext : DbContext
{
    public TraSuaContext(DbContextOptions<TraSuaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }

    public virtual DbSet<DanhMucMon> DanhMucMons { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<MonUong> MonUongs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTietHoaDon>(entity =>
        {
            entity.HasKey(e => new { e.MaHd, e.MaMon });

            entity.ToTable("ChiTietHoaDon");

            entity.Property(e => e.MaHd).HasColumnName("MaHD");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 0)");

            entity.HasOne(d => d.MaHdNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MaHd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietHoa__MaHD__5812160E");

            entity.HasOne(d => d.MaMonNavigation).WithMany(p => p.ChiTietHoaDons)
                .HasForeignKey(d => d.MaMon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChiTietHo__MaMon__59063A47");
        });

        modelBuilder.Entity<DanhMucMon>(entity =>
        {
            entity.HasKey(e => e.MaDm).HasName("PK__DanhMucM__2725866EE55E21EB");

            entity.ToTable("DanhMucMon");

            entity.Property(e => e.MaDm).HasColumnName("MaDM");
            entity.Property(e => e.TenDm)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("TenDM");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHd).HasName("PK__HoaDon__2725A6E01AEFA0F2");

            entity.ToTable("HoaDon");

            entity.Property(e => e.MaHd)
                .HasDefaultValueSql("(NEXT VALUE FOR [dbo].[HoaDonNumber])", "DF_HoaDon_MaHD")
                .HasColumnName("MaHD");
            entity.Property(e => e.HinhThucTt)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("HinhThucTT");
            entity.Property(e => e.NgayLap).HasColumnType("datetime");
            entity.Property(e => e.TongTien).HasColumnType("decimal(18, 0)");
        });

        modelBuilder.Entity<MonUong>(entity =>
        {
            entity.HasKey(e => e.MaMon).HasName("PK__MonUong__3A5B29A8B2DB1892");

            entity.ToTable("MonUong");

            entity.Property(e => e.DangBan).HasDefaultValue(true);
            entity.Property(e => e.GiaBan).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.MaDm).HasColumnName("MaDM");
            entity.Property(e => e.TenMon)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(d => d.MaDmNavigation).WithMany(p => p.MonUongs)
                .HasForeignKey(d => d.MaDm)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MonUong__MaDM__4E88ABD4");
        });
        modelBuilder.HasSequence<int>("HoaDonNumber");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
