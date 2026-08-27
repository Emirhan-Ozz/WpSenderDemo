using System.Drawing;
using System.Windows.Forms;

namespace WhatsAppSenderDemo;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(243, 247, 253);
    public static readonly Color Surface = Color.White;
    public static readonly Color Primary = Color.FromArgb(45, 108, 223);
    public static readonly Color PrimaryHover = Color.FromArgb(32, 88, 194);
    public static readonly Color Danger = Color.FromArgb(214, 69, 69);
    public static readonly Color DangerHover = Color.FromArgb(186, 52, 52);
    public static readonly Color Soft = Color.FromArgb(226, 236, 252);
    public static readonly Color SoftHover = Color.FromArgb(209, 225, 250);
    public static readonly Color SoftText = Color.FromArgb(28, 66, 133);
    public static readonly Color Heading = Color.FromArgb(24, 58, 112);
    public static readonly Color Muted = Color.FromArgb(112, 128, 150);
    public static readonly Color Border = Color.FromArgb(206, 220, 240);
    public static readonly Color GridHeader = Color.FromArgb(232, 240, 253);
    public static readonly Color RowSent = Color.FromArgb(238, 246, 255);
    public static readonly Color RowDelivered = Color.FromArgb(226, 244, 235);
    public static readonly Color RowRead = Color.FromArgb(205, 238, 220);
    public static readonly Color RowError = Color.FromArgb(253, 233, 233);

    public static readonly Font Body = new("Segoe UI", 9F);
    public static readonly Font Strong = new("Segoe UI Semibold", 9.5F);
    public static readonly Font Mono = new("Consolas", 9.5F);

    public static Button PrimaryButton(string text) =>
        Build(text, Primary, PrimaryHover, Color.White, Strong);

    public static Button DangerButton(string text) =>
        Build(text, Danger, DangerHover, Color.White, Strong);

    public static Button SoftButton(string text) =>
        Build(text, Soft, SoftHover, SoftText, Body);

    private static Button Build(string text, Color back, Color hover, Color fore, Font font)
    {
        var b = new Button
        {
            Text = text,
            BackColor = back,
            ForeColor = fore,
            Font = font,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Height = 30,
            Padding = new Padding(8, 0, 8, 0)
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = hover;
        b.FlatAppearance.MouseDownBackColor = hover;
        return b;
    }

    public static GroupBox Group(string text) => new()
    {
        Text = text,
        ForeColor = Heading,
        Font = Body,
        BackColor = Surface,
        Padding = new Padding(10)
    };

    public static Label Caption(string text) => new()
    {
        Text = text,
        ForeColor = Heading,
        Font = Body,
        AutoSize = false
    };

    public static void StyleInput(Control c)
    {
        c.BackColor = Surface;
        c.ForeColor = Color.FromArgb(30, 41, 59);
        c.Font = Body;
    }

    public static void StyleGrid(DataGridView g)
    {
        g.BackgroundColor = Surface;
        g.BorderStyle = BorderStyle.None;
        g.GridColor = Border;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Heading;
        g.ColumnHeadersDefaultCellStyle.Font = Strong;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeader;
        g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Heading;
        g.ColumnHeadersHeight = 32;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 229, 250);
        g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 40, 80);
        g.RowTemplate.Height = 26;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
    }
}
