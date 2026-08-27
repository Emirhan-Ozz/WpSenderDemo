using System;
using System.Windows.Forms;

namespace WhatsAppSenderDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // .NET 6+ ile gelen kaynak-uretimli baslatici (HighDpi + varsayilan font)
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.ToString(), "Beklenmeyen hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        Application.Run(new MainForm());
    }
}
