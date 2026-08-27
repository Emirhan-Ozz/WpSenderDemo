using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

/// <summary>
/// RESMI YOL: Meta WhatsApp Cloud API.
/// POST https://graph.facebook.com/{version}/{phone-number-id}/messages
/// Authorization: Bearer {access-token}
/// </summary>
public sealed class CloudApiSender : IWhatsAppSender
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly AppSettings _s;

    public CloudApiSender(AppSettings settings) => _s = settings;

    public string DisplayName => "Meta Cloud API (resmi)";

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(_s.PhoneNumberId))
            return "Ayarlar sekmesinde 'Phone Number ID' bos birakilamaz.";
        if (string.IsNullOrWhiteSpace(_s.AccessToken))
            return "Ayarlar sekmesinde 'Access Token' bos birakilamaz.";
        if (string.IsNullOrWhiteSpace(_s.ApiVersion))
            return "API surumu bos birakilamaz (orn. v21.0).";
        return null;
    }

    public async Task<SendOutcome> SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        var url = $"https://graph.facebook.com/{_s.ApiVersion}/{_s.PhoneNumberId}/messages";
        var json = BuildBody(message);

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _s.AccessToken);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var res = await Http.SendAsync(req, ct).ConfigureAwait(false);
            var body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (res.IsSuccessStatusCode)
                return SendOutcome.Ok(ExtractMessageId(body));

            var retryable = res.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)res.StatusCode >= 500;

            return SendOutcome.Fail($"HTTP {(int)res.StatusCode}: {ExtractError(body)}", retryable);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return SendOutcome.Fail("Zaman asimi (30 sn)", retryable: true);
        }
        catch (HttpRequestException ex)
        {
            return SendOutcome.Fail("Ag hatasi: " + ex.Message, retryable: true);
        }
    }

    /// <summary>Cloud API'nin bekledigi JSON govdesini uretir.</summary>
    private static string BuildBody(OutgoingMessage m)
    {
        if (!m.UseTemplate)
        {
            // Serbest metin: SADECE 24 saatlik musteri hizmeti penceresi acikken calisir.
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = m.Phone,
                type = "text",
                text = new { preview_url = false, body = m.Text }
            };
            return JsonSerializer.Serialize(payload);
        }

        // Sablon: pencere kapaliyken / ilk temasta zorunlu yol.
        object template = m.TemplateParameters.Count == 0
            ? new
            {
                name = m.TemplateName,
                language = new { code = m.LanguageCode }
            }
            : new
            {
                name = m.TemplateName,
                language = new { code = m.LanguageCode },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = m.TemplateParameters
                            .Select(p => new { type = "text", text = p })
                            .ToArray()
                    }
                }
            };

        return JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = m.Phone,
            type = "template",
            template
        });
    }

    private static string? ExtractMessageId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("messages", out var msgs) &&
                msgs.ValueKind == JsonValueKind.Array && msgs.GetArrayLength() > 0 &&
                msgs[0].TryGetProperty("id", out var id))
                return id.GetString();
        }
        catch (JsonException) { /* yok say */ }
        return null;
    }

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                var msg = err.TryGetProperty("message", out var m) ? m.GetString() : null;
                var det = err.TryGetProperty("error_data", out var ed) &&
                          ed.TryGetProperty("details", out var d) ? d.GetString() : null;
                var code = err.TryGetProperty("code", out var c) ? c.ToString() : null;
                return $"[{code}] {msg} {det}".Trim();
            }
        }
        catch (JsonException) { /* yok say */ }
        return body.Length > 300 ? body[..300] : body;
    }
}
