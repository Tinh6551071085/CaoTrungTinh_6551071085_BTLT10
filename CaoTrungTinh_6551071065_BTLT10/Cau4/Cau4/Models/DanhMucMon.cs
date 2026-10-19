using System;
using System.Collections.Generic;

namespace Cau4;

public partial class DanhMucMon
{
    public int MaDm { get; set; }

    public string TenDm { get; set; } = null!;

    public virtual ICollection<MonUong> MonUongs { get; set; } = new List<MonUong>();
}
