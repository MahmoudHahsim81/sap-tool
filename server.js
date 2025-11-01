// server.js — Hashim License Server (complete, ES Modules)

import express from 'express';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import jwt from 'jsonwebtoken';
import { v4 as uuidv4 } from 'uuid';
import { createPublicKey } from 'crypto';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
app.use(express.json());

// ====== Config & Paths ======
const DATA_DIR = process.env.DATA_DIR || path.join(__dirname, 'data');
const PRIV_KEY_PATH = process.env.PRIV_KEY_PATH || path.join(DATA_DIR, 'private.pem');
const PUB_KEY_PATH = process.env.PUB_KEY_PATH || path.join(DATA_DIR, 'public.pem');
const PUBLIC_DIR = path.join(__dirname, 'public');
const ADMIN_SECRET = process.env.ADMIN_SECRET || (process.env.NODE_ENV === 'production' ? null : 'dev-secret-change-me');
const TOKEN_TTL_DAYS = parseInt(process.env.TOKEN_TTL_DAYS || '30', 10);

if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });
if (!fs.existsSync(PUBLIC_DIR)) fs.mkdirSync(PUBLIC_DIR, { recursive: true });

// ====== Small helpers ======
const dbPath = path.join(DATA_DIR, 'db.json');

function loadDb() {
    if (!fs.existsSync(dbPath)) return { licenses: {}, revokedJti: [] };
    try { return JSON.parse(fs.readFileSync(dbPath, 'utf8')); }
    catch { return { licenses: {}, revokedJti: [] }; }
}
function saveDb(db) { fs.writeFileSync(dbPath, JSON.stringify(db, null, 2)); }

function requireKeys() {
    if (!fs.existsSync(PRIV_KEY_PATH) || !fs.existsSync(PUB_KEY_PATH)) {
        throw new Error('Missing RSA keys. Run: npm run gen:key');
    }
    return {
        privateKey: fs.readFileSync(PRIV_KEY_PATH, 'utf8'),
        publicKey: fs.readFileSync(PUB_KEY_PATH, 'utf8'),
    };
}
function ok(res, obj) { res.json(obj); }
function bad(res, code, msg) { res.status(code).json({ ok: false, reason: msg }); }
function isAdmin(req) {
    const hdr = req.headers['x-admin-secret'] || req.query.admin;
    return ADMIN_SECRET && hdr === ADMIN_SECRET;
}
function adminOnly(req, res, next) {
    if (!isAdmin(req)) return bad(res, 401, 'admin_only');
    next();
}

// ====== 1) Admin dashboard route (place BEFORE any static) ======
app.get('/admin', (req, res) => {
    const p = path.join(PUBLIC_DIR, 'admin.html');
    console.log('🟢 /admin ->', p, 'exists=', fs.existsSync(p));
    res.sendFile(p);
});
app.get('/admin/', (req, res) => {
    const p = path.join(PUBLIC_DIR, 'admin.html');
    console.log('🟢 /admin/ ->', p, 'exists=', fs.existsSync(p));
    res.sendFile(p);
});

// ====== 2) (optional) serve other static files from /public on root ======
app.use(express.static(PUBLIC_DIR));

// ====== Ping ======
app.get('/', (req, res) => res.send('Hashim License Server is running.'));

// ====== Core APIs ======
app.post('/activate', (req, res) => {
    const { key, machineId, product } = req.body || {};
    if (!key || !machineId) return bad(res, 400, 'key & machineId required');

    const db = loadDb();
    const lic = db.licenses[key];
    if (!lic) return bad(res, 403, 'invalid_key');
    if (lic.status === 'revoked') return bad(res, 403, 'revoked');
    if (lic.product && product && lic.product !== product)
        return bad(res, 403, 'wrong_product');

    if (!lic.machineId) lic.machineId = machineId;
    if (lic.machineId !== machineId) return bad(res, 403, 'machine_mismatch');

    const { privateKey } = requireKeys();
    const jti = uuidv4();
    const exp = Math.floor(Date.now() / 1000) + TOKEN_TTL_DAYS * 24 * 3600;

    const payload = {
        jti, machineId,
        features: lic.features || '',
        product: lic.product || 'HashimSapTool',
        exp
    };
    const token = jwt.sign(payload, privateKey, { algorithm: 'RS256' });

    if (!lic.issued) lic.issued = [];
    lic.issued.push({ jti, exp, at: Date.now() });
    db.licenses[key] = lic;
    saveDb(db);

    ok(res, { token });
});

app.post('/validate', (req, res) => {
    const { token } = req.body || {};
    if (!token) return bad(res, 400, 'token required');

    const { publicKey } = requireKeys();
    try {
        const payload = jwt.verify(token, publicKey, { algorithms: ['RS256'] });
        const db = loadDb();
        if (db.revokedJti.includes(payload.jti)) return bad(res, 403, 'revoked');
        ok(res, { ok: true, payload });
    } catch (e) {
        return bad(res, 403, e.message);
    }
});

// ====== Admin APIs ======
app.get('/admin/db', adminOnly, (req, res) => {
    ok(res, loadDb());
});

app.post('/admin/license', adminOnly, (req, res) => {
    const { key, features, product, status } = req.body || {};
    if (!key) return bad(res, 400, 'key required');

    const db = loadDb();
    if (!db.licenses[key]) {
        db.licenses[key] = {
            features: features || '',
            product: product || 'HashimSapTool',
            status: status || 'active'
        };
    } else {
        if (features !== undefined) db.licenses[key].features = features;
        if (product !== undefined) db.licenses[key].product = product;
        if (status !== undefined) db.licenses[key].status = status;
    }
    saveDb(db);
    ok(res, { ok: true, license: db.licenses[key] });
});

app.post('/admin/revoke-key', adminOnly, (req, res) => {
    const { key } = req.body || {};
    if (!key) return bad(res, 400, 'key required');
    const db = loadDb();
    if (!db.licenses[key]) return bad(res, 404, 'not_found');
    db.licenses[key].status = 'revoked';
    saveDb(db);
    ok(res, { ok: true });
});

app.post('/admin/revoke-jti', adminOnly, (req, res) => {
    const { jti } = req.body || {};
    if (!jti) return bad(res, 400, 'jti required');
    const db = loadDb();
    if (!db.revokedJti.includes(jti)) db.revokedJti.push(jti);
    saveDb(db);
    ok(res, { ok: true });
});

// ====== Public RSA XML for C# ======
app.get('/admin/public-xml', adminOnly, (req, res) => {
    const pem = fs.readFileSync(PUB_KEY_PATH, 'utf8');
    const key = createPublicKey(pem);
    const der = key.export({ type: 'pkcs1', format: 'der' }); // RSAPublicKey (mod, exp)

    // Minimal DER reader (INTEGERs of modulus & exponent)
    function rdLen(buf, i) {
        let len = buf[i + 1];
        if (len & 0x80) {
            const n = len & 0x7f; len = 0;
            for (let k = 0; k < n; k++) len = (len << 8) | buf[i + 2 + k];
            return { len, skip: 2 + n };
        }
        return { len, skip: 2 };
    }
    function rdInt(buf, i) {
        if (buf[i] !== 0x02) throw new Error('Expected INTEGER');
        const { len, skip } = rdLen(buf, i);
        const s = i + skip;
        let b = buf.slice(s, s + len);
        if (b[0] === 0x00) b = b.slice(1);
        return { val: b, next: s + len };
    }

    const b = Buffer.from(der);
    if (b[0] !== 0x30) return bad(res, 500, 'Bad DER');
    let p = 0; const L = rdLen(b, p); p += L.skip;
    const mod = rdInt(b, p); p = mod.next;
    const exp = rdInt(b, p);

    const b64 = x => Buffer.from(x).toString('base64');
    const xml = `<RSAKeyValue><Modulus>${b64(mod.val)}</Modulus><Exponent>${b64(exp.val)}</Exponent></RSAKeyValue>`;
    res.type('text/plain').send(xml);
});

// ====== Start ======
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log('License server listening on :' + PORT);
    console.log('PUBLIC_DIR =', PUBLIC_DIR);
});
