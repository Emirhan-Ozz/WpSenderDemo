namespace WhatsAppSenderDemo.Models;

/// <summary>
/// Uygulama ayarlari. %APPDATA%\WhatsAppSenderDemo\settings.json icinde saklanir.
/// AccessToken alani diske yazilirken DPAPI ile sifrelenir (bkz. SettingsStore).
/// </summary>
public class AppSettings
{
    /// <summary>cloud | bridge | walink</summary>
    public string Provider { get; set; } = "cloud";

    // --- Meta Cloud API ---
    public string ApiVersion { get; set; } = "v21.0";
    public string PhoneNumberId { get; set; } = "";
    public string AccessToken { get; set; } = "";

    // --- Yerel kopru (whatsapp-web.js) ---
    public string BridgeUrl { get; set; } = "http://localhost:3000";
    public string BridgeApiKey { get; set; } = "degistir-beni";

    // --- Gonderim davranisi ---
    /// <summary>Basinda ulke kodu olmayan numaralara eklenecek kod (Turkiye = 90).</summary>
    public string DefaultCountryCode { get; set; } = "90";

    /// <summary>Iki mesaj arasindaki temel bekleme (ms).</summary>
    public int DelayMs { get; set; } = 4000;

    /// <summary>Beklemeye eklenecek rastgele sapma (ms). Spam algilanmayi azaltir.</summary>
    public int JitterMs { get; set; } = 2000;

    /// <summary>Gecici hatalarda (429/5xx) kac kez yeniden denensin.</summary>
    public int MaxRetry { get; set; } = 2;
}
