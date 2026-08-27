using System.Net;
using System.Text;
using System.Text.Json;

namespace WhatsAppSenderDemo.Services;

public sealed class WebhookServer : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private string _verifyToken = "";

    public event Action<WebhookStatus>? StatusReceived;
    public event Action<IncomingMessage>? MessageReceived;
    public event Action<string>? Log;

    public bool IsRunning => _listener is { IsListening: true };

    public int Port { get; private set; }

    public void Start(int port, string verifyToken)
    {
        Stop();

        Port = port;
        _verifyToken = verifyToken ?? "";

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => LoopAsync(_cts.Token));

        Log?.Invoke($"Dinleniyor: http://localhost:{port}/webhook");
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener is { IsListening: true })
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }

            try { await HandleAsync(ctx, ct).ConfigureAwait(false); }
            catch (Exception ex) { Log?.Invoke("Hata: " + ex.Message); }
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        if (req.HttpMethod == "GET")
        {
            var mode = req.QueryString["hub.mode"];
            var token = req.QueryString["hub.verify_token"];
            var challenge = req.QueryString["hub.challenge"] ?? "";

            if (mode == "subscribe" && token == _verifyToken)
            {
                Log?.Invoke("Doğrulama başarılı.");
                await WriteAsync(res, 200, challenge).ConfigureAwait(false);
            }
            else
            {
                Log?.Invoke($"Doğrulama reddedildi. Gelen anahtar: {token}");
                await WriteAsync(res, 403, "forbidden").ConfigureAwait(false);
            }
            return;
        }

        if (req.HttpMethod == "POST")
        {
            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
                body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

            await WriteAsync(res, 200, "EVENT_RECEIVED").ConfigureAwait(false);

            try { Parse(body); }
            catch (Exception ex) { Log?.Invoke("Gövde ayrıştırılamadı: " + ex.Message); }
            return;
        }

        await WriteAsync(res, 405, "method not allowed").ConfigureAwait(false);
    }

    private static async Task WriteAsync(HttpListenerResponse res, int status, string text)
    {
        res.StatusCode = status;
        res.ContentType = "text/plain; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(text);
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        res.Close();
    }

    private void Parse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("entry", out var entries)) return;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes)) continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;

                if (value.TryGetProperty("statuses", out var statuses) &&
                    statuses.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in statuses.EnumerateArray())
                    {
                        var id = Str(s, "id");
                        var status = Str(s, "status");
                        var to = Str(s, "recipient_id");
                        var time = Unix(Str(s, "timestamp"));

                        string? error = null;
                        if (s.TryGetProperty("errors", out var errs) &&
                            errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
                        {
                            var e = errs[0];
                            var code = e.TryGetProperty("code", out var c) ? c.ToString() : "";
                            var title = Str(e, "title");
                            var detail = e.TryGetProperty("error_data", out var ed)
                                ? Str(ed, "details") : "";
                            error = $"[{code}] {title} {detail}".Trim();
                        }

                        if (!string.IsNullOrEmpty(id))
                            StatusReceived?.Invoke(new WebhookStatus(id, status, to, time, error));
                    }
                }

                if (value.TryGetProperty("messages", out var messages) &&
                    messages.ValueKind == JsonValueKind.Array)
                {
                    foreach (var m in messages.EnumerateArray())
                    {
                        var from = Str(m, "from");
                        var text = m.TryGetProperty("text", out var t) ? Str(t, "body") : "";
                        var type = Str(m, "type");
                        if (string.IsNullOrEmpty(text)) text = $"({type})";
                        MessageReceived?.Invoke(new IncomingMessage(from, text, Unix(Str(m, "timestamp"))));
                    }
                }
            }
        }
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static DateTime Unix(string seconds) =>
        long.TryParse(seconds, out var s)
            ? DateTimeOffset.FromUnixTimeSeconds(s).LocalDateTime
            : DateTime.Now;

    public void Dispose() => Stop();
}

public record WebhookStatus(string MessageId, string Status, string Recipient, DateTime Time, string? Error)
{
    public string Turkish => Status switch
    {
        "sent" => "İletildi",
        "delivered" => "Ulaştı",
        "read" => "Okundu",
        "failed" => "Başarısız",
        "deleted" => "Silindi",
        _ => Status
    };
}

public record IncomingMessage(string From, string Text, DateTime Time);
