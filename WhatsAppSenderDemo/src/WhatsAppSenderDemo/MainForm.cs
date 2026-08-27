using System.Text;
using WhatsAppSenderDemo.Models;
using WhatsAppSenderDemo.Services;

namespace WhatsAppSenderDemo;

public sealed class MainForm : Form
{
    private AppSettings _settings = SettingsStore.Load();
    private CancellationTokenSource? _cts;

    private readonly WebhookServer _webhook = new();
    private readonly Dictionary<string, int> _rowByMessageId = new(StringComparer.Ordinal);

    private TabControl tabs = null!;
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

    private TextBox txtPhoneNumberId = null!;
    private TextBox txtAccessToken = null!;
    private TextBox txtApiVersion = null!;
    private TextBox txtBridgeUrl = null!;
    private TextBox txtBridgeKey = null!;
    private TextBox txtCountryCode = null!;
    private NumericUpDown numJitter = null!;
    private NumericUpDown numRetry = null!;

    private NumericUpDown numWebhookPort = null!;
    private TextBox txtWebhookToken = null!;
    private TextBox txtWebhookLog = null!;
    private Button btnWebhookToggle = null!;
    private Label lblWebhookState = null!;

    public MainForm()
    {
        Text = "WhatsApp Toplu Mesaj";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1080, 760);
        MinimumSize = new Size(940, 660);
        Font = Theme.Body;
        BackColor = Theme.Background;

        BuildUi();
        LoadSettingsToUi();
        UpdateRecipientCount();
        UpdateProviderUi();
        WireWebhook();
    }

    private void BuildUi()
    {
        tabs = new TabControl { Dock = DockStyle.Fill, Font = Theme.Body };
        tabs.TabPages.Add(BuildSendTab());
        tabs.TabPages.Add(BuildSettingsTab());

        var status = new StatusStrip { BackColor = Theme.GridHeader, SizingGrip = false };
        lblStatus = new ToolStripStatusLabel("Hazır") { ForeColor = Theme.Heading };
        status.Items.Add(lblStatus);

        Controls.Add(tabs);
        Controls.Add(status);
    }

    private TabPage BuildSendTab()
    {
        var page = new TabPage("Gönderim")
        {
            Padding = new Padding(10),
            BackColor = Theme.Background
        };

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));

        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        var grpTo = Theme.Group("Alıcılar");
        grpTo.Dock = DockStyle.Fill;

        txtRecipients = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = Theme.Mono,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "05321234567;Ahmet",
            WordWrap = false
        };
        Theme.StyleInput(txtRecipients);
        txtRecipients.Font = Theme.Mono;
        txtRecipients.TextChanged += (_, _) => UpdateRecipientCount();

        var toButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 6, 0, 0)
        };
        var btnImport = Theme.SoftButton("Dosyadan Yükle");
        btnImport.Click += BtnImport_Click;
        var btnClear = Theme.SoftButton("Temizle");
        btnClear.Click += (_, _) => txtRecipients.Clear();
        lblRecipientCount = new Label
        {
            Text = "0 alıcı",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Padding = new Padding(12, 8, 0, 0)
        };
        toButtons.Controls.AddRange(new Control[] { btnImport, btnClear, lblRecipientCount });

        grpTo.Controls.Add(txtRecipients);
        grpTo.Controls.Add(toButtons);

        var grpMsg = Theme.Group("Mesaj");
        grpMsg.Dock = DockStyle.Fill;

        txtMessage = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Merhaba {ad}, ..."
        };
        Theme.StyleInput(txtMessage);
        txtMessage.TextChanged += (_, _) =>
            lblCharCount.Text = $"{txtMessage.TextLength} / 4096 karakter";

        lblCharCount = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            Text = "0 / 4096 karakter",
            ForeColor = Theme.Muted,
            Padding = new Padding(0, 6, 0, 0)
        };

        grpMsg.Controls.Add(txtMessage);
        grpMsg.Controls.Add(lblCharCount);

        top.Controls.Add(grpTo, 0, 0);
        top.Controls.Add(grpMsg, 1, 0);

        var mid = Theme.Group("Gönderim Ayarları");
        mid.Dock = DockStyle.Fill;
        var midPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
        var actions = new Panel { Dock = DockStyle.Right, Width = 152, BackColor = Theme.Surface };

        midPanel.Controls.Add(new Label
        {
            Text = "Yöntem",
            Left = 4,
            Top = 10,
            Width = 55,
            ForeColor = Theme.Heading
        });
        var cmbFrame = new Panel
        {
            Left = 62,
            Top = 6,
            Width = 280,
            Height = 25,
            BackColor = Theme.Border,
            Padding = new Padding(1)
        };
        cmbProvider = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface
        };
        cmbProvider.Items.AddRange(new object[]
        {
            new ProviderItem("cloud",  "Meta Cloud API"),
            new ProviderItem("bridge", "Yerel Köprü (whatsapp-web.js)"),
            new ProviderItem("walink", "wa.me Bağlantısı")
        });
        cmbProvider.SelectedIndexChanged += (_, _) => UpdateProviderUi();
        cmbFrame.Controls.Add(cmbProvider);
        midPanel.Controls.Add(cmbFrame);

        midPanel.Controls.Add(new Label
        {
            Text = "Bekleme (ms)",
            Left = 356,
            Top = 10,
            Width = 90,
            ForeColor = Theme.Heading
        });
        numDelay = new NumericUpDown
        {
            Left = 450,
            Top = 6,
            Width = 80,
            Minimum = 0,
            Maximum = 600000,
            Increment = 500,
            Value = 4000,
            BorderStyle = BorderStyle.FixedSingle
        };
        midPanel.Controls.Add(numDelay);

        chkTemplate = new CheckBox
        {
            Text = "Şablon mesajı gönder",
            Left = 4,
            Top = 46,
            Width = 160,
            ForeColor = Theme.Heading
        };
        chkTemplate.CheckedChanged += (_, _) => UpdateProviderUi();
        midPanel.Controls.Add(chkTemplate);

        midPanel.Controls.Add(new Label { Text = "Ad", Left = 170, Top = 48, Width = 24, ForeColor = Theme.Heading });
        txtTemplateName = new TextBox { Left = 196, Top = 44, Width = 140, BorderStyle = BorderStyle.FixedSingle };
        midPanel.Controls.Add(txtTemplateName);

        midPanel.Controls.Add(new Label { Text = "Dil", Left = 344, Top = 48, Width = 24, ForeColor = Theme.Heading });
        txtTemplateLang = new TextBox { Left = 370, Top = 44, Width = 50, BorderStyle = BorderStyle.FixedSingle, Text = "tr" };
        midPanel.Controls.Add(txtTemplateLang);

        midPanel.Controls.Add(new Label { Text = "Parametreler", Left = 430, Top = 48, Width = 82, ForeColor = Theme.Heading });
        txtTemplateParams = new TextBox
        {
            Left = 514,
            Top = 44,
            Width = 180,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "{ad}|12345"
        };
        midPanel.Controls.Add(txtTemplateParams);

        progress = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Height = 16,
            ForeColor = Theme.Primary,
            Style = ProgressBarStyle.Continuous
        };
        midPanel.Controls.Add(progress);

        btnSend = Theme.PrimaryButton("Gönder");
        btnSend.SetBounds(10, 2, 134, 38);
        btnSend.Font = new Font("Segoe UI Semibold", 11F);
        btnSend.Click += BtnSend_Click;
        actions.Controls.Add(btnSend);

        btnStop = Theme.DangerButton("Durdur");
        btnStop.SetBounds(10, 46, 134, 38);
        btnStop.Font = new Font("Segoe UI Semibold", 11F);
        btnStop.Click += (_, _) =>
        {
            if (_cts is null)
            {
                lblStatus.Text = "Devam eden bir gönderim yok.";
                return;
            }
            _cts.Cancel();
            lblStatus.Text = "Gönderim durduruluyor...";
        };
        actions.Controls.Add(btnStop);

        mid.Controls.Add(midPanel);
        mid.Controls.Add(actions);

        var grpLog = Theme.Group("Sonuçlar");
        grpLog.Dock = DockStyle.Fill;

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        Theme.StyleGrid(dgv);

        dgv.Columns.Add("c_no", "#");
        dgv.Columns.Add("c_time", "Saat");
        dgv.Columns.Add("c_phone", "Numara");
        dgv.Columns.Add("c_name", "Ad");
        dgv.Columns.Add("c_status", "Durum");
        dgv.Columns.Add("c_delivery", "Teslim");
        dgv.Columns.Add("c_id", "Mesaj Kimliği");
        dgv.Columns.Add("c_error", "Açıklama");
        dgv.Columns["c_no"]!.FillWeight = 24;
        dgv.Columns["c_time"]!.FillWeight = 44;
        dgv.Columns["c_phone"]!.FillWeight = 80;
        dgv.Columns["c_name"]!.FillWeight = 70;
        dgv.Columns["c_status"]!.FillWeight = 56;
        dgv.Columns["c_delivery"]!.FillWeight = 60;
        dgv.Columns["c_id"]!.FillWeight = 96;
        dgv.Columns["c_error"]!.FillWeight = 150;

        var logButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 6, 0, 0)
        };
        var btnExport = Theme.SoftButton("CSV Olarak Kaydet");
        btnExport.Click += BtnExport_Click;
        var btnClearLog = Theme.SoftButton("Listeyi Temizle");
        btnClearLog.Click += (_, _) => { dgv.Rows.Clear(); _rowByMessageId.Clear(); };
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
        var page = new TabPage("Ayarlar")
        {
            Padding = new Padding(14),
            AutoScroll = true,
            BackColor = Theme.Background
        };
        int y = 10;

        var grpCloud = Theme.Group("Meta Cloud API");
        grpCloud.SetBounds(10, y, 920, 150);

        grpCloud.Controls.Add(new Label { Text = "Telefon Numarası Kimliği", Left = 16, Top = 34, Width = 180, ForeColor = Theme.Heading });
        txtPhoneNumberId = new TextBox { Left = 204, Top = 30, Width = 320, BorderStyle = BorderStyle.FixedSingle };
        grpCloud.Controls.Add(txtPhoneNumberId);

        grpCloud.Controls.Add(new Label { Text = "Erişim Anahtarı", Left = 16, Top = 70, Width = 180, ForeColor = Theme.Heading });
        txtAccessToken = new TextBox { Left = 204, Top = 66, Width = 560, UseSystemPasswordChar = true, BorderStyle = BorderStyle.FixedSingle };
        grpCloud.Controls.Add(txtAccessToken);
        var chkShow = new CheckBox { Text = "Göster", Left = 774, Top = 68, Width = 70, ForeColor = Theme.Heading };
        chkShow.CheckedChanged += (_, _) => txtAccessToken.UseSystemPasswordChar = !chkShow.Checked;
        grpCloud.Controls.Add(chkShow);

        grpCloud.Controls.Add(new Label { Text = "Graph API Sürümü", Left = 16, Top = 106, Width = 180, ForeColor = Theme.Heading });
        txtApiVersion = new TextBox { Left = 204, Top = 102, Width = 100, Text = "v21.0", BorderStyle = BorderStyle.FixedSingle };
        grpCloud.Controls.Add(txtApiVersion);

        var btnTestCloud = Theme.SoftButton("Bağlantıyı Test Et");
        btnTestCloud.SetBounds(324, 100, 150, 28);
        btnTestCloud.Click += BtnTestCloud_Click;
        grpCloud.Controls.Add(btnTestCloud);

        page.Controls.Add(grpCloud);
        y += 162;

        var grpBridge = Theme.Group("Yerel Köprü");
        grpBridge.SetBounds(10, y, 920, 112);

        grpBridge.Controls.Add(new Label { Text = "Köprü Adresi", Left = 16, Top = 34, Width = 180, ForeColor = Theme.Heading });
        txtBridgeUrl = new TextBox { Left = 204, Top = 30, Width = 320, Text = "http://localhost:3000", BorderStyle = BorderStyle.FixedSingle };
        grpBridge.Controls.Add(txtBridgeUrl);

        grpBridge.Controls.Add(new Label { Text = "API Anahtarı", Left = 16, Top = 70, Width = 180, ForeColor = Theme.Heading });
        txtBridgeKey = new TextBox { Left = 204, Top = 66, Width = 320, BorderStyle = BorderStyle.FixedSingle };
        grpBridge.Controls.Add(txtBridgeKey);

        var btnTestBridge = Theme.SoftButton("Köprü Durumunu Sorgula");
        btnTestBridge.SetBounds(542, 64, 190, 28);
        btnTestBridge.Click += BtnTestBridge_Click;
        grpBridge.Controls.Add(btnTestBridge);

        page.Controls.Add(grpBridge);
        y += 124;

        var grpHook = Theme.Group("Webhook");
        grpHook.SetBounds(10, y, 920, 240);

        grpHook.Controls.Add(new Label { Text = "Yerel Port", Left = 16, Top = 34, Width = 180, ForeColor = Theme.Heading });
        numWebhookPort = new NumericUpDown
        {
            Left = 204,
            Top = 30,
            Width = 90,
            Minimum = 1024,
            Maximum = 65535,
            Value = 5005,
            BorderStyle = BorderStyle.FixedSingle
        };
        grpHook.Controls.Add(numWebhookPort);

        grpHook.Controls.Add(new Label { Text = "Doğrulama Anahtarı", Left = 320, Top = 34, Width = 130, ForeColor = Theme.Heading });
        txtWebhookToken = new TextBox { Left = 452, Top = 30, Width = 220, Text = "winformdemo-gizli", BorderStyle = BorderStyle.FixedSingle };
        grpHook.Controls.Add(txtWebhookToken);

        btnWebhookToggle = Theme.SoftButton("Webhook'u Başlat");
        btnWebhookToggle.SetBounds(690, 28, 160, 28);
        btnWebhookToggle.Click += BtnWebhookToggle_Click;
        grpHook.Controls.Add(btnWebhookToggle);

        lblWebhookState = new Label
        {
            Left = 16,
            Top = 66,
            Width = 880,
            Height = 18,
            Text = "Durum: kapalı",
            ForeColor = Theme.Muted
        };
        grpHook.Controls.Add(lblWebhookState);

        txtWebhookLog = new TextBox
        {
            Left = 16,
            Top = 92,
            Width = 880,
            Height = 132,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 8.5F),
            BackColor = Theme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
        grpHook.Controls.Add(txtWebhookLog);

        page.Controls.Add(grpHook);
        y += 252;

        var grpGen = Theme.Group("Genel");
        grpGen.SetBounds(10, y, 920, 140);

        grpGen.Controls.Add(new Label { Text = "Varsayılan Ülke Kodu", Left = 16, Top = 34, Width = 180, ForeColor = Theme.Heading });
        txtCountryCode = new TextBox { Left = 204, Top = 30, Width = 80, Text = "90", BorderStyle = BorderStyle.FixedSingle };
        grpGen.Controls.Add(txtCountryCode);

        grpGen.Controls.Add(new Label { Text = "Rastgele Sapma (ms)", Left = 16, Top = 70, Width = 180, ForeColor = Theme.Heading });
        numJitter = new NumericUpDown
        {
            Left = 204,
            Top = 66,
            Width = 90,
            Minimum = 0,
            Maximum = 60000,
            Increment = 250,
            Value = 2000,
            BorderStyle = BorderStyle.FixedSingle
        };
        grpGen.Controls.Add(numJitter);

        grpGen.Controls.Add(new Label { Text = "Yeniden Deneme", Left = 330, Top = 70, Width = 120, ForeColor = Theme.Heading });
        numRetry = new NumericUpDown
        {
            Left = 456,
            Top = 66,
            Width = 60,
            Minimum = 0,
            Maximum = 5,
            Value = 2,
            BorderStyle = BorderStyle.FixedSingle
        };
        grpGen.Controls.Add(numRetry);

        var btnSave = Theme.SoftButton("Ayarları Kaydet");
        btnSave.SetBounds(16, 100, 150, 30);
        btnSave.Click += (_, _) => SaveSettings(showMessage: true);
        grpGen.Controls.Add(btnSave);

        var btnSaveReturn = Theme.PrimaryButton("Ayarları Kaydet ve Gönderime Dön");
        btnSaveReturn.SetBounds(176, 100, 250, 30);
        btnSaveReturn.Click += (_, _) =>
        {
            SaveSettings(showMessage: false);
            tabs.SelectedIndex = 0;
            lblStatus.Text = "Ayarlar kaydedildi.";
        };
        grpGen.Controls.Add(btnSaveReturn);

        page.Controls.Add(grpGen);

        return page;
    }

    private sealed class ProviderItem
    {
        public string Key { get; }
        private readonly string _label;
        public ProviderItem(string key, string label) { Key = key; _label = label; }
        public override string ToString() => _label;
    }

    private void SaveSettings(bool showMessage)
    {
        ReadSettingsFromUi();
        SettingsStore.Save(_settings);
        lblStatus.Text = "Ayarlar kaydedildi.";
        if (showMessage)
            MessageBox.Show("Ayarlar kaydedildi.", "Bilgi",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

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
        numWebhookPort.Value = Math.Clamp(_settings.WebhookPort,
            (int)numWebhookPort.Minimum, (int)numWebhookPort.Maximum);
        txtWebhookToken.Text = _settings.WebhookVerifyToken;

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
        _settings.WebhookPort = (int)numWebhookPort.Value;
        _settings.WebhookVerifyToken = txtWebhookToken.Text.Trim();
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
        lblRecipientCount.Text = $"{valid} geçerli / {list.Count} satır";
        lblRecipientCount.ForeColor = valid == list.Count ? Theme.Muted : Theme.Danger;
    }

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Metin ve CSV dosyaları (*.txt;*.csv)|*.txt;*.csv|Tüm dosyalar (*.*)|*.*",
            Title = "Alıcı Listesi Seç"
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
            MessageBox.Show("Dosya okunamadı: " + ex.Message, "Hata",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnTestCloud_Click(object? sender, EventArgs e)
    {
        ReadSettingsFromUi();
        var cloud = new CloudApiSender(_settings);
        var err = cloud.Validate();
        if (err != null) { MessageBox.Show(err, "Eksik Ayar"); return; }

        var to = Prompt("Test mesajı hangi numaraya gönderilsin?", "");
        if (string.IsNullOrWhiteSpace(to)) return;

        lblStatus.Text = "Test mesajı gönderiliyor...";
        var msg = new OutgoingMessage
        {
            Phone = PhoneUtils.Normalize(to, _settings.DefaultCountryCode),
            UseTemplate = true,
            TemplateName = "hello_world",
            LanguageCode = "en_US"
        };
        var res = await cloud.SendAsync(msg, CancellationToken.None);
        lblStatus.Text = res.Success ? "Test mesajı gönderildi." : "Test başarısız.";
        MessageBox.Show(res.Success
                ? $"Başarılı. Mesaj kimliği: {res.MessageId}"
                : "Hata: " + res.Error,
            "Cloud API Testi", MessageBoxButtons.OK,
            res.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private async void BtnTestBridge_Click(object? sender, EventArgs e)
    {
        ReadSettingsFromUi();
        var b = new BridgeSender(_settings);
        lblStatus.Text = "Köprü sorgulanıyor...";
        var (ready, info) = await b.CheckStatusAsync(CancellationToken.None);
        lblStatus.Text = info;
        MessageBox.Show(ready ? "Köprü hazır. " + info : "Köprü hazır değil. " + info,
            "Köprü Durumu", MessageBoxButtons.OK,
            ready ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        ReadSettingsFromUi();

        var recipients = PhoneUtils.Parse(txtRecipients.Text, _settings.DefaultCountryCode);
        if (recipients.Count == 0)
        {
            MessageBox.Show("Önce alıcı ekleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var useTemplate = chkTemplate.Checked && CurrentProviderKey() == "cloud";
        if (!useTemplate && string.IsNullOrWhiteSpace(txtMessage.Text))
        {
            MessageBox.Show("Mesaj metni boş.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var whatsappSender = CreateSender();
        var validationError = whatsappSender.Validate();
        if (validationError != null)
        {
            MessageBox.Show(validationError, "Eksik Ayar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var valid = recipients.Count(r => r.IsValid);
        var confirm = MessageBox.Show(
            $"{valid} alıcıya mesaj gönderilecek.\nYöntem: {whatsappSender.DisplayName}\nDevam edilsin mi?",
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
            lblStatus.Text = $"{progress.Value} / {progress.Maximum} işlendi";
        });

        var engine = new BulkSender(whatsappSender, _settings);

        try
        {
            var (sent, failed) = await engine.RunAsync(
                recipients, txtMessage.Text, templateOptions, progressReporter, _cts.Token);

            lblStatus.Text = $"Tamamlandı. Başarılı: {sent}, Hatalı: {failed}";
            MessageBox.Show($"Gönderim tamamlandı.\n\nBaşarılı: {sent}\nHatalı: {failed}",
                "Bitti", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Gönderim durduruldu.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Hata: " + ex.Message;
            MessageBox.Show(ex.ToString(), "Beklenmeyen Hata",
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
        btnSend.BackColor = busy ? Theme.PrimaryMuted : Theme.Primary;
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
            "",
            r.MessageId,
            r.Error);

        dgv.Rows[i].DefaultCellStyle.BackColor = r.Success ? Theme.RowSent : Theme.RowError;
        dgv.FirstDisplayedScrollingRowIndex = i;

        if (!string.IsNullOrEmpty(r.MessageId) && r.MessageId.StartsWith("wamid.", StringComparison.Ordinal))
            _rowByMessageId[r.MessageId] = i;
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (dgv.Rows.Count == 0) { MessageBox.Show("Kaydedilecek satır yok."); return; }

        using var dlg = new SaveFileDialog
        {
            Filter = "CSV dosyası (*.csv)|*.csv",
            FileName = $"gonderim_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("No;Saat;Numara;Ad;Durum;Teslim;MesajKimligi;Aciklama");
        foreach (DataGridViewRow row in dgv.Rows)
        {
            var cells = row.Cells.Cast<DataGridViewCell>()
                .Select(c => (c.Value?.ToString() ?? "").Replace(';', ','));
            sb.AppendLine(string.Join(';', cells));
        }

        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        lblStatus.Text = "CSV kaydedildi: " + dlg.FileName;
    }

    private void WireWebhook()
    {
        _webhook.Log += msg => BeginInvoke(() => AppendWebhookLog(msg));

        _webhook.StatusReceived += st => BeginInvoke(() =>
        {
            AppendWebhookLog($"{st.Time:HH:mm:ss}  {st.Recipient}  →  {st.Turkish}" +
                             (st.Error is null ? "" : "  | " + st.Error));
            ApplyDeliveryStatus(st);
        });

        _webhook.MessageReceived += m => BeginInvoke(() =>
        {
            AppendWebhookLog($"{m.Time:HH:mm:ss}  GELEN  {m.From}: {m.Text}");
            lblStatus.Text = $"{m.From} numarasından mesaj geldi, 24 saatlik pencere açıldı.";
        });
    }

    private void BtnWebhookToggle_Click(object? sender, EventArgs e)
    {
        if (_webhook.IsRunning)
        {
            _webhook.Stop();
            btnWebhookToggle.Text = "Webhook'u Başlat";
            lblWebhookState.Text = "Durum: kapalı";
            lblWebhookState.ForeColor = Theme.Muted;
            AppendWebhookLog("Webhook durduruldu.");
            return;
        }

        ReadSettingsFromUi();
        try
        {
            _webhook.Start(_settings.WebhookPort, _settings.WebhookVerifyToken);
            SettingsStore.Save(_settings);

            btnWebhookToggle.Text = "Webhook'u Durdur";
            lblWebhookState.Text =
                $"Durum: çalışıyor   ·   http://localhost:{_settings.WebhookPort}/webhook" +
                $"   ·   Dışarı açmak için:  ngrok http {_settings.WebhookPort}";
            lblWebhookState.ForeColor = Theme.Primary;
        }
        catch (Exception ex)
        {
            AppendWebhookLog("Başlatılamadı: " + ex.Message);
            MessageBox.Show(
                "Webhook başlatılamadı: " + ex.Message +
                "\n\nPort başka bir uygulama tarafından kullanılıyor olabilir. Farklı bir port deneyin.",
                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyDeliveryStatus(WebhookStatus st)
    {
        if (!_rowByMessageId.TryGetValue(st.MessageId, out var rowIndex)) return;
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;

        var row = dgv.Rows[rowIndex];
        row.Cells["c_delivery"].Value = st.Turkish;

        if (!string.IsNullOrEmpty(st.Error))
            row.Cells["c_error"].Value = st.Error;

        row.DefaultCellStyle.BackColor = st.Status switch
        {
            "read" => Theme.RowRead,
            "delivered" => Theme.RowDelivered,
            "failed" => Theme.RowError,
            _ => row.DefaultCellStyle.BackColor
        };
    }

    private void AppendWebhookLog(string line)
    {
        if (txtWebhookLog.TextLength > 20000) txtWebhookLog.Clear();
        txtWebhookLog.AppendText(line + Environment.NewLine);
    }

    private string Prompt(string text, string defaultValue)
    {
        using var f = new Form
        {
            Text = "Giriş",
            ClientSize = new Size(430, 150),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            BackColor = Theme.Background,
            Font = Theme.Body
        };
        var lbl = new Label { Text = text, Left = 14, Top = 16, Width = 400, Height = 40, ForeColor = Theme.Heading };
        var tb = new TextBox { Left = 14, Top = 62, Width = 400, Text = defaultValue, BorderStyle = BorderStyle.FixedSingle };
        var ok = Theme.PrimaryButton("Tamam");
        ok.SetBounds(238, 100, 84, 30);
        ok.DialogResult = DialogResult.OK;
        var cancel = Theme.SoftButton("İptal");
        cancel.SetBounds(330, 100, 84, 30);
        cancel.DialogResult = DialogResult.Cancel;
        f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
        f.AcceptButton = ok;
        f.CancelButton = cancel;
        return f.ShowDialog(this) == DialogResult.OK ? tb.Text.Trim() : "";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_cts is { IsCancellationRequested: false })
        {
            var r = MessageBox.Show("Gönderim devam ediyor. Kapatılsın mı?", "Uyarı",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r == DialogResult.No) { e.Cancel = true; return; }
            _cts.Cancel();
        }

        _webhook.Dispose();

        ReadSettingsFromUi();
        SettingsStore.Save(_settings);
        base.OnFormClosing(e);
    }
}