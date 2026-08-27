# WhatsApp Toplu Mesaj Demo (C# WinForms)

Ekranda yazılan mesajı, listedeki birden çok alıcıya WhatsApp üzerinden gönderen
.NET 8 WinForms demo uygulaması. Üç farklı gönderim yöntemi tek arayüzde:

| # | Yöntem | Ücret | Resmî mi? | Kullanım |
|---|--------|-------|-----------|----------|
| 1 | Meta Cloud API | Test numarasıyla ücretsiz, üretimde mesaj başı ücret | ✅ Evet | Ticari / üretim |
| 2 | Yerel köprü (whatsapp-web.js) | Tamamen ücretsiz | ❌ Hayır (ban riski) | Test / iç kullanım |
| 3 | wa.me bağlantısı | Ücretsiz | ✅ (resmî link şeması) | Tek tek, yarı otomatik |

## Klasör yapısı

```
WhatsAppSenderDemo/
├─ WhatsAppSenderDemo.sln
├─ src/WhatsAppSenderDemo/         → WinForms uygulaması
│  ├─ Program.cs
│  ├─ MainForm.cs                  → tüm arayüz (kod ile kuruluyor)
│  ├─ Models/                      → AppSettings, Recipient, OutgoingMessage
│  └─ Services/
│     ├─ IWhatsAppSender.cs        → ortak arayüz
│     ├─ CloudApiSender.cs         → Meta Cloud API
│     ├─ BridgeSender.cs           → yerel Node köprüsü
│     ├─ WaLinkSender.cs           → wa.me
│     ├─ BulkSender.cs             → toplu gönderim motoru
│     ├─ PhoneUtils.cs             → numara normalize + {ad} yer tutucu
│     └─ SettingsStore.cs          → ayarlar (token DPAPI ile şifreli)
├─ bridge/                         → Node.js köprüsü (whatsapp-web.js)
│  ├─ package.json
│  └─ server.js
└─ ornek-aliciler.csv
```

## Hızlı başlangıç

### 1. Uygulamayı çalıştır
```bash
git clone <repo>            # veya zip'i açın
cd WhatsAppSenderDemo
dotnet restore
dotnet run --project src/WhatsAppSenderDemo
```
Veya `WhatsAppSenderDemo.sln` dosyasını Visual Studio 2022 ile açıp F5.

Gereksinim: **.NET 8 SDK** + Windows.

### 2. Yöntem seç

**A) Meta Cloud API (resmî)**
1. https://developers.facebook.com → My Apps → Create App → *Business*
2. Ürünlere **WhatsApp** ekleyin → otomatik bir **test numarası** verilir
3. *API Setup* ekranından **Phone Number ID** ve **Access Token**'ı kopyalayın
4. Aynı ekranda "To" kısmına kendi numaranızı ekleyip SMS kodunu doğrulayın (en fazla 5 numara)
5. Uygulamada **Ayarlar** sekmesine değerleri yapıştırın → *Bağlantıyı test et*
6. İlk mesaj **şablon** olmalı: `Şablon mesajı gönder` işaretli, ad `hello_world`, dil `en_US`
7. Alıcı size cevap yazınca **24 saat** boyunca serbest metin gönderebilirsiniz

**B) Yerel köprü (ücretsiz)**
```bash
cd bridge
npm install
npm start          # terminaldeki QR kodu telefondan okutun
```
Uygulamada yöntem olarak *Yerel köprü*'yü seçin, Ayarlar → *Köprü durumunu sorgula*.

**C) wa.me** — hiçbir kurulum gerekmez, her alıcı için pencere açılır, GÖNDER'e siz basarsınız.

### 3. Alıcı listesi
Metin kutusuna satır başına bir numara yazın veya `Dosyadan yükle` ile CSV alın:
```
05321234567;Ahmet Yılmaz
+90 533 111 22 33;Ayşe Demir
905341112233
# bu satır yorum
```
Ülke kodu yoksa Ayarlar'daki kod (varsayılan `90`) otomatik eklenir.
Mesajda `{ad}` ve `{tel}` yer tutucuları kullanılabilir.

## Webhook — teslim durumu (isteğe bağlı)

Cloud API'de `200 OK` yalnızca "Meta mesajı kabul etti" demektir. Mesajın gerçekten
ulaşıp ulaşmadığını ancak webhook söyler. Uygulama bunu kendi içinde barındırır
(`Services/WebhookServer.cs`, ayrı proje gerekmez):

1. **Ayarlar → Webhook** bölümünde port (`5005`) ve verify token'ı belirleyip **Webhook'u başlat**
2. Ayrı bir terminalde tüneli açın:
   ```bash
   ngrok http 5005
   ```
3. ngrok'un verdiği `https://xxxx.ngrok-free.app` adresini kopyalayın
4. Meta → uygulamanız → **WhatsApp → Configuration → Webhooks → Edit**
   - **Callback URL:** `https://xxxx.ngrok-free.app/webhook`
   - **Verify token:** Ayarlar'daki token ile birebir aynı
5. **Verify and save** → sonra `messages` alanına **Subscribe**

Artık sonuç tablosundaki **Teslim** sütunu canlı güncellenir:
`İletildi → Ulaştı → Okundu`, hata varsa `Başarısız` ve sebebi. Gelen mesajlar da
webhook günlüğüne düşer — o an alıcının 24 saatlik penceresi açılmış demektir.

## Uyarılar
- **Access token'ı asla kaynak koda / git'e koymayın.** Uygulama token'ı `%APPDATA%\WhatsAppSenderDemo\settings.json` içinde Windows DPAPI ile şifreleyerek saklar.
- whatsapp-web.js resmî değildir; WhatsApp kullanım şartlarına aykırıdır ve numara engellenebilir.
- İzinsiz toplu mesaj göndermek hem WhatsApp politikalarına hem de KVKK'ya aykırıdır. Alıcı onayı (opt-in) alın.
