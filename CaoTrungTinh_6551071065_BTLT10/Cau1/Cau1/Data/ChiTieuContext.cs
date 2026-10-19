using Microsoft.EntityFrameworkCore;
using Cau1.Models;

namespace Cau1.Data;

public partial class ChiTieuContext : DbContext
{
    public ChiTieuContext(DbContextOptions<ChiTieuContext> options) : base(options) { }

    public virtual DbSet<ChiTieu> ChiTieus { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChiTieu>(entity =>
        {
            entity.HasKey(e => e.MaCt);
            entity.ToTable("ChiTieu");
            entity.Property(e => e.MaCt).HasColumnName("MaCT");
            entity.Property(e => e.GhiChu).HasMaxLength(200);
            entity.Property(e => e.LoaiChi).HasMaxLength(30);
            entity.Property(e => e.NoiDung).HasMaxLength(100);
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 0)");
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
