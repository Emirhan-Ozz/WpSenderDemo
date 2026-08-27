using System;
using System.Windows.Forms;

namespace WhatsAppSenderDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.ToString(), "Beklenmeyen Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        Application.Run(new MainForm());
    }
}
