using System;
using System.Windows.Forms;

namespace KonuBot
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Yakalanmamış exception'ları yakala — sessiz kapanmayı önle
            Application.ThreadException += (_, args) =>
            {
                MessageBox.Show(
                    args.Exception.ToString(),
                    "Beklenmeyen Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    args.ExceptionObject?.ToString() ?? "Bilinmeyen hata",
                    "Kritik Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
