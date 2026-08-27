using System.Text;
using System.Text.RegularExpressions;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

/// <summary>
/// Numara temizleme/normalize etme. WhatsApp API'si numarayi
/// "+" ISARETI OLMADAN, ulke kodu dahil bekler. Ornek: 905321234567
/// </summary>
public static class PhoneUtils
{
    public static string OnlyDigits(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            if (char.IsDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    /// <summary>
    /// 0532 123 45 67 / +90 532 123 45 67 / 00905321234567 / 5321234567
    /// hepsini 905321234567 haline getirir.
    /// </summary>
    public static string Normalize(string raw, string defaultCountryCode)
    {
        var d = OnlyDigits(raw);
        if (d.Length == 0) return "";

        if (d.StartsWith("00")) d = d[2..];                 // 0090... -> 90...
        if (d.StartsWith(defaultCountryCode + "0"))          // 900532... -> 90532...
            d = defaultCountryCode + d[(defaultCountryCode.Length + 1)..];
        else if (d.StartsWith("0")) d = defaultCountryCode + d[1..];  // 0532... -> 90532...
        else if (!d.StartsWith(defaultCountryCode) && d.Length <= 10)
            d = defaultCountryCode + d;                      // 532... -> 90532...

        return d;
    }

    /// <summary>Metin kutusundaki satirlari alici listesine cevirir.</summary>
    /// <remarks>
    /// Desteklenen satir bicimleri:
    ///   05321234567
    ///   05321234567;Ahmet Yilmaz
    ///   05321234567,Ahmet Yilmaz
    ///   05321234567 TAB Ahmet Yilmaz
    /// "#" ile baslayan satirlar ve bos satirlar atlanir.
    /// </remarks>
    public static List<Recipient> Parse(string text, string defaultCountryCode)
    {
        var list = new List<Recipient>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var parts = line.Split(new[] { ';', ',', '\t' }, 2);
            var rawPhone = parts[0].Trim();
            var name = parts.Length > 1 ? parts[1].Trim() : "";

            var r = new Recipient { RawPhone = rawPhone, Name = name };
            r.Phone = Normalize(rawPhone, defaultCountryCode);

            if (r.Phone.Length < 10 || r.Phone.Length > 15)
            {
                r.IsValid = false;
                r.ValidationError = "Numara uzunlugu gecersiz (10-15 hane olmali)";
            }
            else if (!seen.Add(r.Phone))
            {
                r.IsValid = false;
                r.ValidationError = "Tekrar eden numara";
            }
            else
            {
                r.IsValid = true;
            }

            list.Add(r);
        }

        return list;
    }

    private static readonly Regex PlaceholderRx =
        new(@"\{(ad|isim|tel|numara)\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Sablondaki {ad} / {tel} yer tutucularini alicinin bilgileriyle degistirir.</summary>
    public static string ApplyPlaceholders(string template, Recipient r) =>
        PlaceholderRx.Replace(template, m => m.Groups[1].Value.ToLowerInvariant() switch
        {
            "ad" or "isim" => string.IsNullOrWhiteSpace(r.Name) ? "" : r.Name,
            _ => r.Phone
        });
}
