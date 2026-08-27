namespace WhatsAppSenderDemo.Models;

public class OutgoingMessage
{
    public string Phone { get; set; } = "";
    public string Text { get; set; } = "";

    public bool UseTemplate { get; set; }
    public string TemplateName { get; set; } = "hello_world";
    public string LanguageCode { get; set; } = "en_US";
    public List<string> TemplateParameters { get; set; } = new();
}

public record SendOutcome(bool Success, string? MessageId, string? Error, bool Retryable = false)
{
    public static SendOutcome Ok(string? id) => new(true, id, null);
    public static SendOutcome Fail(string error, bool retryable = false) => new(false, null, error, retryable);
}

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
