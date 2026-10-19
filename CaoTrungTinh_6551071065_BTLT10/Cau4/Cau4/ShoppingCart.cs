namespace Cau4;

public sealed record CartItem(int MaMon, string TenMon, int SoLy, decimal DonGia)
{
    public decimal ThanhTien => SoLy * DonGia;
}

public sealed class ShoppingCart
{
    private readonly List<CartItem> items = new();
    public IReadOnlyList<CartItem> Items => items.AsReadOnly();
    public decimal Total => items.Sum(x => x.ThanhTien);

    public void Add(int id, string name, int quantity, decimal price)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(name) || quantity <= 0 || price <= 0 || decimal.Truncate(price) != price)
            throw new ArgumentException("Thông tin món không hợp lệ.");
        var index = items.FindIndex(x => x.MaMon == id);
        if (index < 0) items.Add(new CartItem(id, name, quantity, price));
        else
        {
            var current = items[index];
            items[index] = current with { SoLy = checked(current.SoLy + quantity) };
        }
    }
    public void Remove(int id) => items.RemoveAll(x => x.MaMon == id);
    public void Clear() => items.Clear();
}
