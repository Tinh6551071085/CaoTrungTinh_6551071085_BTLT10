using Serilog;

namespace Cau2;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cau2", "Logs");
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
            .WriteTo.File(Path.Combine(logFolder, "nhatro-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Lỗi ngoài luồng giao diện. IsTerminating={Terminating}", e.IsTerminating);
            MessageBox.Show("Đã xảy ra lỗi, thông tin đã được ghi lại", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log.CloseAndFlush();
        };
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
        catch (Exception ex) { ReportError(ex); }
        finally { Log.CloseAndFlush(); }
    }

    private static void ReportError(Exception ex)
    {
        Log.Error(ex, "Lỗi chưa được xử lý");
        MessageBox.Show("Đã xảy ra lỗi, thông tin đã được ghi lại", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
