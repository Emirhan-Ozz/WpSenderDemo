using System.Diagnostics;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

public sealed class WaLinkSender : IWhatsAppSender
{
    public string DisplayName => "wa.me Bağlantısı";

    public string? Validate() => null;

    public Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        try
        {
            var url = $"https://wa.me/{message.Phone}?text={Uri.EscapeDataString(message.Text)}";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            return Task.FromResult(SendOutcome.Ok("Pencere açıldı"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SendOutcome.Fail(ex.Message));
        }
    }
}
