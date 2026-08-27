namespace WhatsAppSenderDemo.Models;

/// <summary>Gonderilecek tek mesaj. Duz metin veya onayli sablon olabilir.</summary>
public class OutgoingMessage
{
    public string Phone { get; set; } = "";
    public string Text { get; set; } = "";

    // --- Sablon (template) modu: 24 saatlik pencere disinda zorunlu ---
    public bool UseTemplate { get; set; }
    public string TemplateName { get; set; } = "hello_world";
    public string LanguageCode { get; set; } = "en_US";

    /// <summary>Sablon govdesindeki {{1}}, {{2}}... yerine gecen degerler.</summary>
    public List<string> TemplateParameters { get; set; } = new();
}

/// <summary>Tek gonderimin sonucu.</summary>
public record SendOutcome(bool Success, string? MessageId, string? Error, bool Retryable = false)
{
    public static SendOutcome Ok(string? id) => new(true, id, null);
    public static SendOutcome Fail(string error, bool retryable = false) => new(false, null, error, retryable);
}

/// <summary>Log tablosuna yazilan satir.</summary>
public class SendResult
{
    public int Index { get; set; }
    public string Phone { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Success { get; set; }
    public string Status { get; set; } = "";
    public string MessageId { get; set; } = "";
    public string Error { get; set; } = "";
    public DateTime Time { get; set; } = DateTime.Now;
}
