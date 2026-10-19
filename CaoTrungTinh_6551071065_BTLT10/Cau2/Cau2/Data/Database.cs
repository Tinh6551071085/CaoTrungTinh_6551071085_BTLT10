using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Cau2.Data;

public static class Database
{
    public static QlNhaTroContext Open()
    {
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
        var connection = config.RootElement.GetProperty("ConnectionString").GetString()
            ?? throw new InvalidOperationException("Thiếu chuỗi kết nối CSDL.");
        return new QlNhaTroContext(new DbContextOptionsBuilder<QlNhaTroContext>()
            .UseSqlServer(connection, options => options.CommandTimeout(10)).Options);
    }
}
