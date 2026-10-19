using System;
using System.Collections.Generic;

namespace Cau2.Models;

public partial class PhongTro
{
    public int MaPhong { get; set; }

    public string TenPhong { get; set; } = null!;

    public decimal GiaThue { get; set; }

    public string TinhTrang { get; set; } = null!;

    public virtual ICollection<KhachThue> KhachThue { get; set; } = new List<KhachThue>();
}
