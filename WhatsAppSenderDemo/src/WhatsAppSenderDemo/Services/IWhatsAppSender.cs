using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

/// <summary>
/// Tum gonderim yontemlerinin ortak arayuzu.
/// Boylece formdaki kod, hangi altyapinin kullanildigini bilmek zorunda kalmaz.
/// </summary>
public interface IWhatsAppSender
{
    string DisplayName { get; }

    /// <summary>Gonderim oncesi ayar kontrolu. Hata varsa mesaji dondurur, yoksa null.</summary>
    string? Validate();

    Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct);
}
