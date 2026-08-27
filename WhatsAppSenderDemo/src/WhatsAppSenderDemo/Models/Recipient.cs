namespace WhatsAppSenderDemo.Models;

public class Recipient
{
    public string RawPhone { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? Phone : $"{Phone} ({Name})";
}
