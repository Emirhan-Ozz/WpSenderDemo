using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

/// <summary>
/// Coklu (toplu) gonderim motoru: sirayla gonderir, bekler,
/// gecici hatalarda yeniden dener, ilerlemeyi bildirir, iptal edilebilir.
/// </summary>
public sealed class BulkSender
{
    private readonly IWhatsAppSender _sender;
    private readonly AppSettings _settings;
    private readonly Random _rnd = new();

    public BulkSender(IWhatsAppSender sender, AppSettings settings)
    {
        _sender = sender;
        _settings = settings;
    }

    /// <param name="messageTemplate">{ad} / {tel} yer tutucusu icerebilen metin.</param>
    /// <param name="templateOptions">Cloud API sablon modu icin (null ise duz metin).</param>
    public async Task<(int Sent, int Failed)> RunAsync(
        IReadOnlyList<Recipient> recipients,
        string messageTemplate,
        OutgoingMessage? templateOptions,
        IProgress<SendResult> progress,
        CancellationToken ct)
    {
        int sent = 0, failed = 0, index = 0;

        foreach (var r in recipients)
        {
            ct.ThrowIfCancellationRequested();
            index++;

            if (!r.IsValid)
            {
                failed++;
                progress.Report(new SendResult
                {
                    Index = index,
                    Phone = r.RawPhone,
                    Name = r.Name,
                    Success = false,
                    Status = "Atlandi",
                    Error = r.ValidationError ?? "Gecersiz numara"
                });
                continue;
            }

            var msg = new OutgoingMessage
            {
                Phone = r.Phone,
                Text = PhoneUtils.ApplyPlaceholders(messageTemplate, r)
            };

            if (templateOptions is { UseTemplate: true })
            {
                msg.UseTemplate = true;
                msg.TemplateName = templateOptions.TemplateName;
                msg.LanguageCode = templateOptions.LanguageCode;
                msg.TemplateParameters = templateOptions.TemplateParameters
                    .Select(p => PhoneUtils.ApplyPlaceholders(p, r))
                    .ToList();
            }

            SendOutcome outcome = SendOutcome.Fail("baslatilmadi");

            for (var attempt = 0; attempt <= _settings.MaxRetry; attempt++)
            {
                outcome = await _sender.SendAsync(msg, ct).ConfigureAwait(false);
                if (outcome.Success || !outcome.Retryable) break;

                // Ustel bekleme: 2sn, 4sn, 8sn...
                var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                await Task.Delay(backoff, ct).ConfigureAwait(false);
            }

            if (outcome.Success) sent++; else failed++;

            progress.Report(new SendResult
            {
                Index = index,
                Phone = r.Phone,
                Name = r.Name,
                Success = outcome.Success,
                Status = outcome.Success ? "Gonderildi" : "Hata",
                MessageId = outcome.MessageId ?? "",
                Error = outcome.Error ?? ""
            });

            // Son aliciysa bekleme
            if (index < recipients.Count)
            {
                var wait = _settings.DelayMs + _rnd.Next(0, Math.Max(1, _settings.JitterMs));
                await Task.Delay(wait, ct).ConfigureAwait(false);
            }
        }

        return (sent, failed);
    }
}
