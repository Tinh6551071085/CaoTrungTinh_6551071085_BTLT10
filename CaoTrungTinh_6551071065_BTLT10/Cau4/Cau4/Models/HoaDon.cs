using System;
using System.Collections.Generic;

namespace Cau4;

public partial class HoaDon
{
    public int MaHd { get; set; }

    public DateTime NgayLap { get; set; }

    public decimal TongTien { get; set; }

    public string HinhThucTt { get; set; } = null!;

    public bool DaThanhToan { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();
}
