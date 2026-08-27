using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

/// <summary>
/// UCRETSIZ YOL: bilgisayarda calisan Node.js koprusu (whatsapp-web.js).
/// Kopru, kendi WhatsApp hesabinizla WhatsApp Web oturumu acar; WinForms
/// uygulamasi ona basit bir HTTP istegi yollar.
///
/// DIKKAT: Resmi olmayan yontemdir, WhatsApp kullanim sartlarina aykiridir
/// ve numaranin engellenmesi riski vardir. Test/ic kullanim icindir.
/// </summary>
public sealed class BridgeSender : IWhatsAppSender
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly AppSettings _s;

    public BridgeSender(AppSettings settings) => _s = settings;

    public string DisplayName => "Yerel kopru - whatsapp-web.js (ucretsiz)";

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(_s.BridgeUrl))
            return "Kopru adresi bos olamaz (orn. http://localhost:3000).";
        if (!Uri.TryCreate(_s.BridgeUrl, UriKind.Absolute, out _))
            return "Kopru adresi gecerli bir URL degil.";
        return null;
    }

    /// <summary>Kopru ayakta mi ve WhatsApp oturumu acik mi?</summary>
    public async Task<(bool Ready, string Info)> CheckStatusAsync(CancellationToken ct)
    {
        try
        {
            using var res = await Http.GetAsync(_s.BridgeUrl.TrimEnd('/') + "/status", ct)
                                      .ConfigureAwait(false);
            var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode) return (false, $"HTTP {(int)res.StatusCode}");

            using var doc = JsonDocument.Parse(body);
            var state = doc.RootElement.TryGetProperty("state", out var s) ? s.GetString() : "?";
            var ready = doc.RootElement.TryGetProperty("ready", out var r) && r.GetBoolean();
            return (ready, $"Durum: {state}");
        }
        catch (Exception ex)
        {
            return (false, "Koprüye ulasilamadi: " + ex.Message);
        }
    }

    public async Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        var url = _s.BridgeUrl.TrimEnd('/') + "/send";
        var json = JsonSerializer.Serialize(new { to = message.Phone, message = message.Text });

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("x-api-key", _s.BridgeApiKey);

        try
        {
            using var res = await Http.SendAsync(req, ct).ConfigureAwait(false);
            var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (res.IsSuccessStatusCode)
            {
                string? id = null;
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("id", out var idEl)) id = idEl.GetString();
                }
                catch (JsonException) { }
                return SendOutcome.Ok(id);
            }

            var retryable = res.StatusCode == HttpStatusCode.ServiceUnavailable ||
                            (int)res.StatusCode >= 500;
            return SendOutcome.Fail($"HTTP {(int)res.StatusCode}: {Trim(body)}", retryable);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return SendOutcome.Fail("Zaman asimi", retryable: true);
        }
        catch (HttpRequestException ex)
        {
            return SendOutcome.Fail("Kopruye baglanilamadi: " + ex.Message, retryable: true);
        }
    }

    private static string Trim(string s) => s.Length > 300 ? s[..300] : s;
}
