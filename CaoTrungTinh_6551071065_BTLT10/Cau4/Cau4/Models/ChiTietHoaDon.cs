using System;
using System.Collections.Generic;

namespace Cau4;

public partial class ChiTietHoaDon
{
    public int MaHd { get; set; }

    public int MaMon { get; set; }

    public int SoLy { get; set; }

    public decimal DonGia { get; set; }

    public virtual HoaDon MaHdNavigation { get; set; } = null!;

    public virtual MonUong MaMonNavigation { get; set; } = null!;
}
