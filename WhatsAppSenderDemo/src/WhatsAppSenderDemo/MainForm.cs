using System.Text;
using WhatsAppSenderDemo.Models;
using WhatsAppSenderDemo.Services;

namespace WhatsAppSenderDemo;

/// <summary>
/// Tek formluk demo. Arayuz tamamen kod ile kuruluyor; boylece projeyi
/// acar acmaz calisir, Designer dosyasi ile ugrasmaniza gerek kalmaz.
/// </summary>
public sealed class MainForm : Form
{
    // --- Durum ---
    private AppSettings _settings = SettingsStore.Load();
    private CancellationTokenSource? _cts;

    // --- Gonderim sekmesi kontrolleri ---
    private TextBox txtRecipients = null!;
    private TextBox txtMessage = null!;
    private Label lblRecipientCount = null!;
    private Label lblCharCount = null!;
    private ComboBox cmbProvider = null!;
    private NumericUpDown numDelay = null!;
    private CheckBox chkTemplate = null!;
    private TextBox txtTemplateName = null!;
    private TextBox txtTemplateLang = null!;
    private TextBox txtTemplateParams = null!;
    private Button btnSend = null!;
    private Button btnStop = null!;
    private ProgressBar progress = null!;
    private DataGridView dgv = null!;
    private ToolStripStatusLabel lblStatus = null!;

    // --- Ayarlar sekmesi kontrolleri ---
    private TextBox txtPhoneNumberId = null!;
    private TextBox txtAccessToken = null!;
    private TextBox txtApiVersion = null!;
    private TextBox txtBridgeUrl = null!;
    private TextBox txtBridgeKey = null!;
    private TextBox txtCountryCode = null!;
    private NumericUpDown numJitter = null!;
    private NumericUpDown numRetry = null!;

    public MainForm()
    {
        Text = "WhatsApp Toplu Mesaj Demo";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1060, 740);
        MinimumSize = new Size(900, 640);
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        LoadSettingsToUi();
        UpdateRecipientCount();
        UpdateProviderUi();
    }

    // ==================================================================
    //  ARAYUZ
    // ==================================================================
    private void BuildUi()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildSendTab());
        tabs.TabPages.Add(BuildSettingsTab());
        tabs.TabPages.Add(BuildHelpTab());

        var status = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("Hazir");
        status.Items.Add(lblStatus);

        Controls.Add(tabs);
        Controls.Add(status);
    }

    private TabPage BuildSendTab()
    {
        var page = new TabPage("Gonderim") { Padding = new Padding(8) };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

        // ---------- Ust: alicilar + mesaj ----------
        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        // --- Alicilar kutusu ---
        var grpTo = new GroupBox { Text = "Alicilar", Dock = DockStyle.Fill, Padding = new Padding(8) };

        txtRecipients = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9.5F),
            PlaceholderText = "05321234567;Ahmet\r\n905331112233;Ayse\r\n# satir basina bir numara",
            WordWrap = false
        };
        txtRecipients.TextChanged += (_, _) => UpdateRecipientCount();

        var toButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            FlowDirection = FlowDirection.LeftToRight
        };
        var btnImport = new Button { Text = "Dosyadan yukle...", AutoSize = true };
        btnImport.Click += BtnImport_Click;
        var btnClear = new Button { Text = "Temizle", AutoSize = true };
        btnClear.Click += (_, _) => txtRecipients.Clear();
        lblRecipientCount = new Label { Text = "0 alici", AutoSize = true, Padding = new Padding(10, 8, 0, 0) };
        toButtons.Controls.AddRange(new Control[] { btnImport, btnClear, lblRecipientCount });

        var toHint = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            ForeColor = SystemColors.GrayText,
            Text = "Bicim:  numara  veya  numara;Ad\r\nUlke kodu yoksa Ayarlar'daki kod otomatik eklenir."
        };

        grpTo.Controls.Add(txtRecipients);
        grpTo.Controls.Add(toButtons);
        grpTo.Controls.Add(toHint);

        // --- Mesaj kutusu ---
        var grpMsg = new GroupBox { Text = "Mesaj", Dock = DockStyle.Fill, Padding = new Padding(8) };

        txtMessage = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "Merhaba {ad}, bu bir test mesajidir."
        };
        txtMessage.TextChanged += (_, _) =>
            lblCharCount.Text = $"{txtMessage.TextLength} karakter (limit 4096)";

        lblCharCount = new Label { Dock = DockStyle.Bottom, Height = 20, Text = "0 karakter (limit 4096)" };

        var msgHint = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            ForeColor = SystemColors.GrayText,
            Text = "Yer tutucular: {ad} -> alicinin adi, {tel} -> numarasi.\r\n" +
                   "Cloud API'de serbest metin yalnizca 24 saatlik pencere ACIKKEN gider."
        };

        grpMsg.Controls.Add(txtMessage);
        grpMsg.Controls.Add(lblCharCount);
        grpMsg.Controls.Add(msgHint);

        top.Controls.Add(grpTo, 0, 0);
        top.Controls.Add(grpMsg, 1, 0);

        // ---------- Orta: kontrol paneli ----------
        var mid = new GroupBox { Text = "Gonderim", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var midPanel = new Panel { Dock = DockStyle.Fill };

        midPanel.Controls.Add(new Label { Text = "Yontem:", Left = 6, Top = 10, Width = 60, TextAlign = ContentAlignment.MiddleLeft });
        cmbProvider = new ComboBox
        {
            Left = 70, Top = 6, Width = 300,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbProvider.Items.AddRange(new object[]
        {
            new ProviderItem("cloud",  "1) Meta Cloud API (resmi)"),
            new ProviderItem("bridge", "2) Yerel kopru / whatsapp-web.js (ucretsiz)"),
            new ProviderItem("walink", "3) wa.me baglantisi (yari otomatik)")
        });
        cmbProvider.SelectedIndexChanged += (_, _) => UpdateProviderUi();
        midPanel.Controls.Add(cmbProvider);

        midPanel.Controls.Add(new Label { Text = "Mesaj arasi bekleme (ms):", Left = 390, Top = 10, Width = 150 });
        numDelay = new NumericUpDown
        {
            Left = 545, Top = 6, Width = 90,
            Minimum = 0, Maximum = 600000, Increment = 500, Value = 4000
        };
        midPanel.Controls.Add(numDelay);

        chkTemplate = new CheckBox
        {
            Text = "Sablon (template) mesaji gonder",
            Left = 6, Top = 44, Width = 230
        };
        chkTemplate.CheckedChanged += (_, _) => UpdateProviderUi();
        midPanel.Controls.Add(chkTemplate);

        midPanel.Controls.Add(new Label { Text = "Ad:", Left = 240, Top = 46, Width = 26 });
        txtTemplateName = new TextBox { Left = 268, Top = 42, Width = 130, Text = "hello_world" };
        midPanel.Controls.Add(txtTemplateName);

        midPanel.Controls.Add(new Label { Text = "Dil:", Left = 404, Top = 46, Width = 26 });
        txtTemplateLang = new TextBox { Left = 432, Top = 42, Width = 60, Text = "en_US" };
        midPanel.Controls.Add(txtTemplateLang);

        midPanel.Controls.Add(new Label { Text = "Parametreler ( | ile ayirin):", Left = 500, Top = 46, Width = 145 });
        txtTemplateParams = new TextBox { Left = 648, Top = 42, Width = 200, PlaceholderText = "{ad}|12.05.2026" };
        midPanel.Controls.Add(txtTemplateParams);

        btnSend = new Button
        {
            Text = "GONDER",
            Left = 660, Top = 2, Width = 120, Height = 32,
            BackColor = Color.FromArgb(37, 211, 102),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };
        btnSend.Click += BtnSend_Click;
        midPanel.Controls.Add(btnSend);

        btnStop = new Button { Text = "Durdur", Left = 790, Top = 2, Width = 90, Height = 32, Enabled = false };
        btnStop.Click += (_, _) => _cts?.Cancel();
        midPanel.Controls.Add(btnStop);

        progress = new ProgressBar { Left = 6, Top = 78, Width = 1000, Height = 18, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
        midPanel.Controls.Add(progress);

        mid.Controls.Add(midPanel);

        // ---------- Alt: sonuc tablosu ----------
        var grpLog = new GroupBox { Text = "Sonuclar", Dock = DockStyle.Fill, Padding = new Padding(8) };
        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        dgv.Columns.Add("c_no", "#");
        dgv.Columns.Add("c_time", "Saat");
        dgv.Columns.Add("c_phone", "Numara");
        dgv.Columns.Add("c_name", "Ad");
        dgv.Columns.Add("c_status", "Durum");
        dgv.Columns.Add("c_id", "Mesaj ID");
        dgv.Columns.Add("c_error", "Aciklama");
        dgv.Columns["c_no"]!.FillWeight = 25;
        dgv.Columns["c_time"]!.FillWeight = 45;
        dgv.Columns["c_phone"]!.FillWeight = 80;
        dgv.Columns["c_name"]!.FillWeight = 70;
        dgv.Columns["c_status"]!.FillWeight = 55;
        dgv.Columns["c_id"]!.FillWeight = 110;
        dgv.Columns["c_error"]!.FillWeight = 180;

        var logButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 34 };
        var btnExport = new Button { Text = "CSV olarak kaydet", AutoSize = true };
        btnExport.Click += BtnExport_Click;
        var btnClearLog = new Button { Text = "Listeyi temizle", AutoSize = true };
        btnClearLog.Click += (_, _) => dgv.Rows.Clear();
        logButtons.Controls.AddRange(new Control[] { btnExport, btnClearLog });

        grpLog.Controls.Add(dgv);
        grpLog.Controls.Add(logButtons);

        root.Controls.Add(top, 0, 0);
        root.Controls.Add(mid, 0, 1);
        root.Controls.Add(grpLog, 0, 2);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildSettingsTab()
    {
        var page = new TabPage("Ayarlar") { Padding = new Padding(12), AutoScroll = true };
        int y = 10;

        // --- Cloud API ---
        var grpCloud = new GroupBox { Text = "Meta Cloud API (resmi yontem)", Left = 10, Top = y, Width = 900, Height = 175 };
        grpCloud.Controls.Add(new Label { Text = "Phone Number ID", Left = 14, Top = 30, Width = 190 });
        txtPhoneNumberId = new TextBox { Left = 210, Top = 26, Width = 320 };
        grpCloud.Controls.Add(txtPhoneNumberId);

        grpCloud.Controls.Add(new Label { Text = "Access Token", Left = 14, Top = 64, Width = 190 });
        txtAccessToken = new TextBox { Left = 210, Top = 60, Width = 560, UseSystemPasswordChar = true };
        grpCloud.Controls.Add(txtAccessToken);
        var chkShow = new CheckBox { Text = "Goster", Left = 780, Top = 62, Width = 70 };
        chkShow.CheckedChanged += (_, _) => txtAccessToken.UseSystemPasswordChar = !chkShow.Checked;
        grpCloud.Controls.Add(chkShow);

        grpCloud.Controls.Add(new Label { Text = "Graph API surumu", Left = 14, Top = 98, Width = 190 });
        txtApiVersion = new TextBox { Left = 210, Top = 94, Width = 100, Text = "v21.0" };
        grpCloud.Controls.Add(txtApiVersion);

        var btnTestCloud = new Button { Text = "Baglantiyi test et", Left = 330, Top = 92, Width = 140 };
        btnTestCloud.Click += BtnTestCloud_Click;
        grpCloud.Controls.Add(btnTestCloud);

        grpCloud.Controls.Add(new Label
        {
            Left = 14, Top = 130, Width = 860, Height = 34, ForeColor = SystemColors.GrayText,
            Text = "developers.facebook.com > uygulamaniz > WhatsApp > API Setup ekranindan alinir. " +
                   "Gecici token 24 saat gecerlidir; kalici token icin System User olusturun."
        });
        page.Controls.Add(grpCloud);
        y += 185;

        // --- Kopru ---
        var grpBridge = new GroupBox { Text = "Yerel kopru (whatsapp-web.js) - ucretsiz yontem", Left = 10, Top = y, Width = 900, Height = 145 };
        grpBridge.Controls.Add(new Label { Text = "Kopru adresi", Left = 14, Top = 30, Width = 190 });
        txtBridgeUrl = new TextBox { Left = 210, Top = 26, Width = 320, Text = "http://localhost:3000" };
        grpBridge.Controls.Add(txtBridgeUrl);

        grpBridge.Controls.Add(new Label { Text = "API anahtari", Left = 14, Top = 64, Width = 190 });
        txtBridgeKey = new TextBox { Left = 210, Top = 60, Width = 320 };
        grpBridge.Controls.Add(txtBridgeKey);

        var btnTestBridge = new Button { Text = "Kopru durumunu sorgula", Left = 545, Top = 58, Width = 180 };
        btnTestBridge.Click += BtnTestBridge_Click;
        grpBridge.Controls.Add(btnTestBridge);

        grpBridge.Controls.Add(new Label
        {
            Left = 14, Top = 98, Width = 860, Height = 34, ForeColor = SystemColors.GrayText,
            Text = "bridge klasorunde 'npm install' ve 'npm start' calistirin, terminaldeki QR kodu " +
                   "telefonunuzdaki WhatsApp > Bagli cihazlar ile okutun."
        });
        page.Controls.Add(grpBridge);
        y += 155;

        // --- Genel ---
        var grpGen = new GroupBox { Text = "Genel", Left = 10, Top = y, Width = 900, Height = 130 };
        grpGen.Controls.Add(new Label { Text = "Varsayilan ulke kodu", Left = 14, Top = 30, Width = 190 });
        txtCountryCode = new TextBox { Left = 210, Top = 26, Width = 80, Text = "90" };
        grpGen.Controls.Add(txtCountryCode);

        grpGen.Controls.Add(new Label { Text = "Rastgele sapma (ms)", Left = 14, Top = 64, Width = 190 });
        numJitter = new NumericUpDown { Left = 210, Top = 60, Width = 90, Minimum = 0, Maximum = 60000, Increment = 250, Value = 2000 };
        grpGen.Controls.Add(numJitter);

        grpGen.Controls.Add(new Label { Text = "Hatada yeniden deneme", Left = 330, Top = 64, Width = 150 });
        numRetry = new NumericUpDown { Left = 485, Top = 60, Width = 60, Minimum = 0, Maximum = 5, Value = 2 };
        grpGen.Controls.Add(numRetry);

        var btnSave = new Button { Text = "Ayarlari kaydet", Left = 14, Top = 95, Width = 140, Height = 28 };
        btnSave.Click += (_, _) =>
        {
            ReadSettingsFromUi();
            SettingsStore.Save(_settings);
            lblStatus.Text = "Ayarlar kaydedildi.";
            MessageBox.Show("Ayarlar kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        grpGen.Controls.Add(btnSave);
        page.Controls.Add(grpGen);

        return page;
    }

    private static TabPage BuildHelpTab()
    {
        var page = new TabPage("Yardim") { Padding = new Padding(10) };
        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9.5F),
            Text = string.Join(Environment.NewLine, new[]
            {
                "HIZLI BASLANGIC",
                "==============",
                "",
                "A) RESMI YOL - Meta Cloud API",
                "   1. developers.facebook.com > My Apps > Create App > 'Business'",
                "   2. Urunlerden WhatsApp'i ekleyin. Otomatik bir TEST numarasi verilir.",
                "   3. API Setup ekranindan Phone Number ID ve Access Token'i kopyalayin.",
                "   4. 'To' alanina kendi numaranizi ekleyip dogrulayin (en fazla 5 numara).",
                "   5. Ayarlar sekmesine bu iki degeri yapistirin, 'Baglantiyi test et'e basin.",
                "   6. Ilk mesaj SABLON olmali: 'Sablon mesaji gonder' kutusunu isaretleyip",
                "      ad = hello_world, dil = en_US birakin.",
                "   7. Alici size cevap verdikten sonra 24 saat boyunca SERBEST METIN",
                "      gonderebilirsiniz (kutu isaretsiz).",
                "",
                "B) UCRETSIZ YOL - Yerel kopru (whatsapp-web.js)",
                "   1. Node.js 18+ kurun.",
                "   2. bridge klasorunde:  npm install   sonra   npm start",
                "   3. Terminalde cikan QR kodu telefonunuzdan okutun.",
                "   4. Ayarlar > Kopru durumunu sorgula -> 'hazir' yaziyorsa gonderebilirsiniz.",
                "   NOT: Resmi olmayan yontemdir; numaranin engellenme riski vardir.",
                "",
                "C) KURULUMSUZ YOL - wa.me baglantisi",
                "   Her alici icin WhatsApp penceresi acilir, GONDER'e siz basarsiniz.",
                "   Gercek toplu gonderim icin uygun degildir.",
                "",
                "SIK KARSILASILAN HATALAR",
                "========================",
                "(131030) Recipient not in allowed list  -> test numarasina alici eklenmemis",
                "(131047) Re-engagement message          -> 24 saatlik pencere kapali, sablon kullanin",
                "(190)    Access token expired           -> gecici token'in suresi doldu",
                "(132001) Template does not exist        -> sablon adi/dili yanlis",
                "(80007 / 429) Rate limit                -> bekleme suresini artirin"
            })
        };
        page.Controls.Add(box);
        return page;
    }

    private sealed class ProviderItem
    {
        public string Key { get; }
        private readonly string _label;
        public ProviderItem(string key, string label) { Key = key; _label = label; }
        public override string ToString() => _label;
    }

    // ==================================================================
    //  AYAR OKU / YAZ
    // ==================================================================
    private void LoadSettingsToUi()
    {
        txtPhoneNumberId.Text = _settings.PhoneNumberId;
        txtAccessToken.Text = _settings.AccessToken;
        txtApiVersion.Text = _settings.ApiVersion;
        txtBridgeUrl.Text = _settings.BridgeUrl;
        txtBridgeKey.Text = _settings.BridgeApiKey;
        txtCountryCode.Text = _settings.DefaultCountryCode;
        numDelay.Value = Math.Clamp(_settings.DelayMs, (int)numDelay.Minimum, (int)numDelay.Maximum);
        numJitter.Value = Math.Clamp(_settings.JitterMs, (int)numJitter.Minimum, (int)numJitter.Maximum);
        numRetry.Value = Math.Clamp(_settings.MaxRetry, (int)numRetry.Minimum, (int)numRetry.Maximum);

        for (var i = 0; i < cmbProvider.Items.Count; i++)
            if (cmbProvider.Items[i] is ProviderItem p && p.Key == _settings.Provider)
                cmbProvider.SelectedIndex = i;
        if (cmbProvider.SelectedIndex < 0) cmbProvider.SelectedIndex = 0;
    }

    private void ReadSettingsFromUi()
    {
        _settings.PhoneNumberId = txtPhoneNumberId.Text.Trim();
        _settings.AccessToken = txtAccessToken.Text.Trim();
        _settings.ApiVersion = txtApiVersion.Text.Trim();
        _settings.BridgeUrl = txtBridgeUrl.Text.Trim();
        _settings.BridgeApiKey = txtBridgeKey.Text.Trim();
        _settings.DefaultCountryCode = PhoneUtils.OnlyDigits(txtCountryCode.Text);
        if (_settings.DefaultCountryCode.Length == 0) _settings.DefaultCountryCode = "90";
        _settings.DelayMs = (int)numDelay.Value;
        _settings.JitterMs = (int)numJitter.Value;
        _settings.MaxRetry = (int)numRetry.Value;
        _settings.Provider = CurrentProviderKey();
    }

    private string CurrentProviderKey() =>
        cmbProvider.SelectedItem is ProviderItem p ? p.Key : "cloud";

    private IWhatsAppSender CreateSender() => CurrentProviderKey() switch
    {
        "bridge" => new BridgeSender(_settings),
        "walink" => new WaLinkSender(),
        _ => new CloudApiSender(_settings)
    };

    private void UpdateProviderUi()
    {
        var isCloud = CurrentProviderKey() == "cloud";
        chkTemplate.Enabled = isCloud;
        if (!isCloud) chkTemplate.Checked = false;

        var t = isCloud && chkTemplate.Checked;
        txtTemplateName.Enabled = t;
        txtTemplateLang.Enabled = t;
        txtTemplateParams.Enabled = t;
        txtMessage.Enabled = !t;
    }

    private void UpdateRecipientCount()
    {
        var cc = PhoneUtils.OnlyDigits(txtCountryCode?.Text ?? "90");
        if (cc.Length == 0) cc = "90";
        var list = PhoneUtils.Parse(txtRecipients.Text, cc);
        var valid = list.Count(r => r.IsValid);
        lblRecipientCount.Text = $"{valid} gecerli / {list.Count} satir";
        lblRecipientCount.ForeColor = valid == list.Count ? SystemColors.ControlText : Color.Firebrick;
    }

    // ==================================================================
    //  OLAYLAR
    // ==================================================================
    private void BtnImport_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Metin/CSV dosyalari (*.txt;*.csv)|*.txt;*.csv|Tum dosyalar (*.*)|*.*",
            Title = "Alici listesi sec"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var text = File.ReadAllText(dlg.FileName, Encoding.UTF8);
            txtRecipients.Text = string.IsNullOrWhiteSpace(txtRecipients.Text)
                ? text
                : txtRecipients.Text.TrimEnd() + Environment.NewLine + text;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Dosya okunamadi: " + ex.Message, "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnTestCloud_Click(object? sender, EventArgs e)
    {
        ReadSettingsFromUi();
        var sender2 = new CloudApiSender(_settings);
        var err = sender2.Validate();
        if (err != null) { MessageBox.Show(err, "Eksik ayar"); return; }

        var to = Prompt("Test mesaji hangi numaraya gonderilsin?\n(Cloud API test numarasinda bu numara onceden eklenmis olmali)", "");
        if (string.IsNullOrWhiteSpace(to)) return;

        lblStatus.Text = "Test mesaji gonderiliyor...";
        var msg = new OutgoingMessage
        {
            Phone = PhoneUtils.Normalize(to, _settings.DefaultCountryCode),
            UseTemplate = true,
            TemplateName = "hello_world",
            LanguageCode = "en_US"
        };
        var res = await sender2.SendAsync(msg, CancellationToken.None);
        lblStatus.Text = res.Success ? "Test mesaji gonderildi." : "Test basarisiz.";
        MessageBox.Show(res.Success
                ? $"Basarili. Mesaj ID: {res.MessageId}"
                : "Hata: " + res.Error,
            "Cloud API testi", MessageBoxButtons.OK,
            res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private async void BtnTestBridge_Click(object? sender, EventArgs e)
    {
        ReadSettingsFromUi();
        var b = new BridgeSender(_settings);
        lblStatus.Text = "Kopru sorgulaniyor...";
        var (ready, info) = await b.CheckStatusAsync(CancellationToken.None);
        lblStatus.Text = info;
        MessageBox.Show(ready ? "Kopru hazir. " + info : "Kopru hazir degil. " + info,
            "Kopru durumu", MessageBoxButtons.OK,
            ready ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        ReadSettingsFromUi();

        var recipients = PhoneUtils.Parse(txtRecipients.Text, _settings.DefaultCountryCode);
        if (recipients.Count == 0)
        {
            MessageBox.Show("Once alici ekleyin.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var useTemplate = chkTemplate.Checked && CurrentProviderKey() == "cloud";
        if (!useTemplate && string.IsNullOrWhiteSpace(txtMessage.Text))
        {
            MessageBox.Show("Mesaj metni bos.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var whatsappSender = CreateSender();
        var validationError = whatsappSender.Validate();
        if (validationError != null)
        {
            MessageBox.Show(validationError, "Eksik ayar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var valid = recipients.Count(r => r.IsValid);
        var confirm = MessageBox.Show(
            $"{valid} aliciya mesaj gonderilecek.\nYontem: {whatsappSender.DisplayName}\nDevam edilsin mi?",
            "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        SettingsStore.Save(_settings);

        OutgoingMessage? templateOptions = null;
        if (useTemplate)
        {
            templateOptions = new OutgoingMessage
            {
                UseTemplate = true,
                TemplateName = txtTemplateName.Text.Trim(),
                LanguageCode = txtTemplateLang.Text.Trim(),
                TemplateParameters = txtTemplateParams.Text
                    .Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToList()
            };
        }

        _cts = new CancellationTokenSource();
        SetBusy(true);
        progress.Value = 0;
        progress.Maximum = recipients.Count;

        var progressReporter = new Progress<SendResult>(r =>
        {
            AppendRow(r);
            if (progress.Value < progress.Maximum) progress.Value++;
            lblStatus.Text = $"{progress.Value}/{progress.Maximum} islendi";
        });

        var engine = new BulkSender(whatsappSender, _settings);

        try
        {
            var (sent, failed) = await engine.RunAsync(
                recipients, txtMessage.Text, templateOptions, progressReporter, _cts.Token);

            lblStatus.Text = $"Tamamlandi. Basarili: {sent}, Hatali: {failed}";
            MessageBox.Show($"Gonderim tamamlandi.\n\nBasarili: {sent}\nHatali: {failed}",
                "Bitti", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Kullanici tarafindan durduruldu.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Hata: " + ex.Message;
            MessageBox.Show(ex.ToString(), "Beklenmeyen hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void SetBusy(bool busy)
    {
        btnSend.Enabled = !busy;
        btnStop.Enabled = busy;
        cmbProvider.Enabled = !busy;
        txtRecipients.ReadOnly = busy;
        Cursor = busy ? Cursors.AppStarting : Cursors.Default;
    }

    private void AppendRow(SendResult r)
    {
        var i = dgv.Rows.Add(
            r.Index,
            r.Time.ToString("HH:mm:ss"),
            r.Phone,
            r.Name,
            r.Status,
            r.MessageId,
            r.Error);

        dgv.Rows[i].DefaultCellStyle.BackColor =
            r.Success ? Color.FromArgb(230, 249, 236) : Color.FromArgb(253, 232, 232);
        dgv.FirstDisplayedScrollingRowIndex = i;
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (dgv.Rows.Count == 0) { MessageBox.Show("Kaydedilecek satir yok."); return; }

        using var dlg = new SaveFileDialog
        {
            Filter = "CSV dosyasi (*.csv)|*.csv",
            FileName = $"gonderim_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("No;Saat;Numara;Ad;Durum;MesajId;Aciklama");
        foreach (DataGridViewRow row in dgv.Rows)
        {
            var cells = row.Cells.Cast<DataGridViewCell>()
                .Select(c => (c.Value?.ToString() ?? "").Replace(';', ','));
            sb.AppendLine(string.Join(';', cells));
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        lblStatus.Text = "CSV kaydedildi: " + dlg.FileName;
    }

    /// <summary>Kucuk bir metin sorma penceresi (WinForms'ta hazir InputBox yok).</summary>
    private string Prompt(string text, string defaultValue)
    {
        using var f = new Form
        {
            Text = "Giris",
            ClientSize = new Size(420, 140),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false
        };
        var lbl = new Label { Text = text, Left = 12, Top = 12, Width = 396, Height = 44 };
        var tb = new TextBox { Left = 12, Top = 62, Width = 396, Text = defaultValue };
        var ok = new Button { Text = "Tamam", Left = 232, Top = 96, Width = 80, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Iptal", Left = 320, Top = 96, Width = 80, DialogResult = DialogResult.Cancel };
        f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        return f.ShowDialog(this) == DialogResult.OK ? tb.Text.Trim() : "";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_cts is { IsCancellationRequested: false } && btnStop.Enabled)
        {
            var r = MessageBox.Show("Gonderim devam ediyor. Kapatilsin mi?", "Uyari",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.No) { e.Cancel = true; return; }
            _cts.Cancel();
        }

        ReadSettingsFromUi();
        SettingsStore.Save(_settings);
        base.OnFormClosing(e);
    }
}
