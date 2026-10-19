using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Cau4;

internal static class TeaDatabase
{
    public static TraSuaContext Open()
    {
        var options = new DbContextOptionsBuilder<TraSuaContext>().UseSqlServer(AppSettings.Load().DatabaseConnection).Options;
        return new TraSuaContext(options);
    }

    public static async Task InitializeAsync()
    {
        var connectionString = new SqlConnectionStringBuilder(AppSettings.Load().DatabaseConnection) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(connectionString.ConnectionString);
        await connection.OpenAsync();
        var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "TraSua.sql"));
        foreach (var batch in Regex.Split(sql, @"^GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    public static async Task<int> ReserveInvoiceNumberAsync()
    {
        await using var connection = new SqlConnection(AppSettings.Load().DatabaseConnection);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT NEXT VALUE FOR dbo.HoaDonNumber", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
