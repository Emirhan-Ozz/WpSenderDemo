using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

public interface IWhatsAppSender
{
    string DisplayName { get; }

    string? Validate();

    Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct);
}
