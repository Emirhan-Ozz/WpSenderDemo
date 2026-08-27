namespace WhatsAppSenderDemo.Models;

/// <summary>Tek bir alici satiri: numara + (istege bagli) ad.</summary>
public class Recipient
{
    public string RawPhone { get; set; } = "";
    public string Phone { get; set; } = "";   // normalize edilmis, E.164 (basinda + yok)
    public string Name { get; set; } = "";
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? Phone : $"{Phone} ({Name})";
}
