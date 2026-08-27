using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

/// <summary>
/// Ayarlari %APPDATA%\WhatsAppSenderDemo\settings.json dosyasina yazar.
/// Access token DPAPI (CurrentUser) ile sifrelenir; baska kullanici okuyamaz.
/// Token'i ASLA kaynak koda veya git'e koymayin.
/// </summary>
public static class SettingsStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WhatsAppSenderDemo");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();

            var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath))
                    ?? new AppSettings();
            s.AccessToken = Unprotect(s.AccessToken);
            return s;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Dir);

        // Kopyasini sifreli token ile yaz, bellekteki nesneyi bozma
        var copy = JsonSerializer.Deserialize<AppSettings>(
                       JsonSerializer.Serialize(settings))!;
        copy.AccessToken = Protect(settings.AccessToken);

        File.WriteAllText(FilePath, JsonSerializer.Serialize(copy, JsonOpts));
    }

    private const string Prefix = "dpapi:";

    private static string Protect(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        try
        {
            var bytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(bytes);
        }
        catch
        {
            return plain; // sifreleme mumkun degilse duz yaz (nadiren)
        }
    }

    private static string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored)) return "";
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;
        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(stored[Prefix.Length..]), null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }
}
