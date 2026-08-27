const express = require('express');
const qrcode = require('qrcode-terminal');
const { Client, LocalAuth } = require('whatsapp-web.js');

const PORT = process.env.PORT || 3000;
const API_KEY = process.env.BRIDGE_API_KEY || 'degistir-beni';

let ready = false;
let state = 'baslatiliyor';
let lastQr = null;

const client = new Client({
  authStrategy: new LocalAuth({ dataPath: './.wwebjs_auth' }),
  puppeteer: {
    headless: true,
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  }
});

client.on('qr', (qr) => {
  lastQr = qr;
  state = 'qr-bekleniyor';
  console.log('\nQR kodu okutun: WhatsApp > Bagli cihazlar\n');
  qrcode.generate(qr, { small: true });
});

client.on('authenticated', () => {
  state = 'kimlik-dogrulandi';
  console.log('[bridge] Kimlik dogrulandi.');
});

client.on('ready', () => {
  ready = true;
  lastQr = null;
  state = 'hazir';
  console.log('[bridge] Oturum hazir.');
});

client.on('auth_failure', (msg) => {
  ready = false;
  state = 'kimlik-hatasi';
  console.error('[bridge] Kimlik dogrulama hatasi:', msg);
});

client.on('disconnected', (reason) => {
  ready = false;
  state = 'baglanti-koptu:' + reason;
  console.warn('[bridge] Baglanti koptu:', reason);
});

client.initialize();

const app = express();
app.use(express.json({ limit: '1mb' }));

function auth(req, res, next) {
  if (req.get('x-api-key') !== API_KEY) {
    return res.status(401).json({ error: 'Gecersiz API anahtari' });
  }
  next();
}

app.get('/status', (req, res) => {
  res.json({ ready, state, qrPending: !!lastQr });
});

app.post('/send', auth, async (req, res) => {
  if (!ready) {
    return res.status(503).json({ error: 'Oturum hazir degil. Durum: ' + state });
  }

  const { to, message } = req.body || {};
  if (!to || !message) {
    return res.status(400).json({ error: '"to" ve "message" alanlari zorunludur' });
  }

  const digits = String(to).replace(/\D/g, '');
  if (digits.length < 10) {
    return res.status(400).json({ error: 'Numara gecersiz: ' + to });
  }

  try {
    const numberId = await client.getNumberId(digits);
    if (!numberId) {
      return res.status(404).json({ error: 'Numara WhatsApp kullanmiyor: ' + digits });
    }

    const sent = await client.sendMessage(numberId._serialized, String(message));
    console.log(`[bridge] Gonderildi: ${digits}`);
    res.json({ ok: true, id: sent.id ? sent.id._serialized : null });
  } catch (err) {
    console.error('[bridge] Gonderim hatasi:', err);
    res.status(500).json({ error: String((err && err.message) || err) });
  }
});

app.listen(PORT, '127.0.0.1', () => {
  console.log(`[bridge] http://127.0.0.1:${PORT}`);
  console.log(`[bridge] API anahtari: ${API_KEY}`);
});
