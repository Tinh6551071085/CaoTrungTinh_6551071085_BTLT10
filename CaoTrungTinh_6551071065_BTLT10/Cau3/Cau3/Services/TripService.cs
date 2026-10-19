using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Cau3.Data;
using Cau3.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cau3.Services;

public sealed class TripService(string connectionString)
{
    private DaNgoaiContext CreateContext() => new(new DbContextOptionsBuilder<DaNgoaiContext>()
        .UseSqlServer(connectionString, options => options.CommandTimeout(8)).Options);

    // SQL script is the source of truth: this is Database First, not EnsureCreated/Migrations.
    public async Task InitializeAsync()
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        string database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidDataException("Database name is required.");
        builder.InitialCatalog = "master";
        await using (var master = new SqlConnection(builder.ConnectionString))
        {
            await master.OpenAsync();
            await using var create = master.CreateCommand();
            create.CommandText = "IF DB_ID(@name) IS NULL BEGIN DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@name); EXEC sp_executesql @sql; END";
            create.Parameters.AddWithValue("@name", database);
            create.CommandTimeout = 15;
            await create.ExecuteNonQueryAsync();
        }
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Cau3.Database.QL_DaNgoai.sql")!;
        using var reader = new StreamReader(stream);
        string sql = await reader.ReadToEndAsync();
        // Skip CREATE DATABASE / USE blocks: the configured database is already selected.
        var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Skip(2);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (string batch in batches.Where(b => !string.IsNullOrWhiteSpace(b)))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 15;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<List<ChuyenDi>> SearchAsync(string query, bool pending, DateOnly? from, DateOnly? to)
    {
        await using var db = CreateContext();
        IQueryable<ChuyenDi> rows = db.ChuyenDis.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query)) rows = rows.Where(t => t.TenChuyen.Contains(query) || t.DiaDiem.Contains(query));
        if (pending) rows = rows.Where(t => !t.DaHoanThanh);
        if (from.HasValue) rows = rows.Where(t => t.NgayDi >= from.Value);
        if (to.HasValue) rows = rows.Where(t => t.NgayDi <= to.Value);
        return await rows.OrderBy(t => t.MaChuyen).ToListAsync();
    }

    public static string? Validate(ChuyenDi trip, bool isNew, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(trip.TenChuyen) || string.IsNullOrWhiteSpace(trip.DiaDiem)) return "required";
        if (trip.TenChuyen.Length > 100 || trip.DiaDiem.Length > 100 || trip.GhiChu?.Length > 200) return "length";
        if (isNew && trip.NgayDi < today) return "past";
        if (trip.SoNguoi < 1 || trip.NganSach < 0 || trip.NganSach > 999999999999999999m) return "numbers";
        if (!Transports.Contains(trip.PhuongTien)) return "transport";
        return null;
    }

    public static readonly string[] Transports = ["Xe máy", "Ô tô", "Xe khách", "Tàu hỏa"];

    public async Task SaveAsync(ChuyenDi trip, bool isNew)
    {
        string? error = Validate(trip, isNew, DateOnly.FromDateTime(DateTime.Today));
        if (error is not null) throw new ArgumentException(error);
        await using var db = CreateContext();
        if (isNew) db.ChuyenDis.Add(trip);
        else
        {
            var original = await db.ChuyenDis.FindAsync(trip.MaChuyen) ?? throw new DBConcurrencyException();
            db.Entry(original).CurrentValues.SetValues(trip);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = CreateContext();
        var trip = await db.ChuyenDis.FindAsync(id) ?? throw new DBConcurrencyException();
        db.ChuyenDis.Remove(trip);
        await db.SaveChangesAsync();
    }
}
