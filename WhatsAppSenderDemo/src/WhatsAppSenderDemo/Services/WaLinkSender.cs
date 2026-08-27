using System.Diagnostics;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

/// <summary>
/// EN BASIT YOL (yari otomatik, tamamen ucretsiz, kurulum yok):
/// https://wa.me/{numara}?text={mesaj} adresini acar.
/// WhatsApp Desktop / WhatsApp Web mesaji hazir sekilde acar,
/// GONDER dugmesine kullanici basar. Toplu gonderimde her alici icin
/// bir pencere acilir; bu yuzden gercek toplu gonderim icin uygun degildir.
/// </summary>
public sealed class WaLinkSender : IWhatsAppSender
{
    public string DisplayName => "wa.me baglantisi (yari otomatik)";

    public string? Validate() => null;

    public Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        try
        {
            var url = $"https://wa.me/{message.Phone}?text={Uri.EscapeDataString(message.Text)}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true   // varsayilan tarayici/WhatsApp Desktop acar
            });
            return Task.FromResult(SendOutcome.Ok("(pencere acildi - GONDER'e basin)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SendOutcome.Fail(ex.Message));
        }
    }
}
