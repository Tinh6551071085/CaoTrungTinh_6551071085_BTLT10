using Cau1.Configuration;
using Cau1.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Cau1
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
                        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                    .CreateLogger();
                var settings = AppSettings.Load();
                var options = new DbContextOptionsBuilder<ChiTieuContext>()
                    .UseSqlServer(settings.ConnectionStrings.ChiTieu).Options;
                Log.Information("Khởi động Sổ chi tiêu cá nhân");
                Application.Run(new Form1(options));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Không khởi động được ứng dụng");
                MessageBox.Show("Không khởi động được ứng dụng. Vui lòng kiểm tra appsettings.json " +
                    "và quyền ghi thư mục Logs cạnh file .exe.", "Sổ chi tiêu cá nhân",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
