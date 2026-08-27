namespace WhatsAppSenderDemo.Models;

public class AppSettings
{
    public string Provider { get; set; } = "cloud";

    public string ApiVersion { get; set; } = "v21.0";
    public string PhoneNumberId { get; set; } = "";
    public string AccessToken { get; set; } = "";

    public string BridgeUrl { get; set; } = "http://localhost:3000";
    public string BridgeApiKey { get; set; } = "degistir-beni";

    public string DefaultCountryCode { get; set; } = "90";
    public int DelayMs { get; set; } = 4000;
    public int JitterMs { get; set; } = 2000;
    public int MaxRetry { get; set; } = 2;

    public int WebhookPort { get; set; } = 5005;
    public string WebhookVerifyToken { get; set; } = "winformdemo-gizli";
    public bool WebhookAutoStart { get; set; }
}
