using Serilog;

namespace Cau4
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
            System.Globalization.CultureInfo.CurrentCulture = TeaUi.Culture;
            System.Globalization.CultureInfo.CurrentUICulture = TeaUi.Culture;
            Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                .WriteTo.File(System.IO.Path.Combine(AppContext.BaseDirectory, "logs", "trasua-.log"),
                    rollingInterval: Serilog.RollingInterval.Day, retainedFileCountLimit: 14)
                .CreateLogger();
            try { Application.Run(new FormBanHang()); }
            catch (Exception ex) { TeaUi.Error(new Form(), ex); }
            finally { Serilog.Log.CloseAndFlush(); }
        }
    }
}
