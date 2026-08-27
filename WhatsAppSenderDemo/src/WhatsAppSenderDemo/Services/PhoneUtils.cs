using System.Text;
using System.Text.RegularExpressions;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

public static class PhoneUtils
{
    public static string OnlyDigits(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            if (char.IsDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    public static string Normalize(string raw, string defaultCountryCode)
    {
        var d = OnlyDigits(raw);
        if (d.Length == 0) return "";

        if (d.StartsWith("00")) d = d[2..];

        if (d.StartsWith(defaultCountryCode + "0"))
            d = defaultCountryCode + d[(defaultCountryCode.Length + 1)..];
        else if (d.StartsWith("0"))
            d = defaultCountryCode + d[1..];
        else if (!d.StartsWith(defaultCountryCode) && d.Length <= 10)
            d = defaultCountryCode + d;

        return d;
    }

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
                r.ValidationError = "Numara uzunluğu geçersiz";
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

    public static string ApplyPlaceholders(string template, Recipient r) =>
        PlaceholderRx.Replace(template, m => m.Groups[1].Value.ToLowerInvariant() switch
        {
            "ad" or "isim" => string.IsNullOrWhiteSpace(r.Name) ? "" : r.Name,
            _ => r.Phone
        });
}
