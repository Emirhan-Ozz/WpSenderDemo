using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WhatsAppSenderDemo.Models;

namespace WhatsAppSenderDemo.Services;

public static class SettingsStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WhatsAppSenderDemo");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private const string Prefix = "dpapi:";

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

        var copy = JsonSerializer.Deserialize<AppSettings>(
                       JsonSerializer.Serialize(settings))!;
        copy.AccessToken = Protect(settings.AccessToken);

        File.WriteAllText(FilePath, JsonSerializer.Serialize(copy, JsonOpts));
    }

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
            return plain;
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
