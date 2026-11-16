#!/usr/bin/env node
// Akıncı tarayıcı eklentisini Chrome Web Store API (v2) ile yükleyip incelemeye gönderir.
//
// Kullanım:
//   node publish-extension.mjs <paket.zip> --version 1.1.0   sürüm mağazada yoksa yükle + incelemeye gönder
//   node publish-extension.mjs <paket.zip> --no-publish      yalnızca yükle (panelde taslak olarak kalır)
//   node publish-extension.mjs --status                      mağazadaki durumu yazdır
//
// Ortam değişkenleri:
//   CWS_SERVICE_ACCOUNT_KEY  Google Cloud hizmet hesabının JSON anahtarı (dosyanın tam içeriği)
//   CWS_PUBLISHER_ID         Chrome Web Store geliştirici panelindeki yayıncı kimliği
//   CWS_EXTENSION_ID         (isteğe bağlı) varsayılan: Akıncı'nın mağaza kimliği
//
// Kimlik doğrulama, Google'ın hizmet hesapları için belgelediği JWT yöntemidir:
//   https://developer.chrome.com/docs/webstore/service-accounts
// API: https://developer.chrome.com/docs/webstore/using-api
// Bağımlılık yoktur; Node 18+ (fetch ve crypto) yeterlidir.

import { createSign } from 'node:crypto';
import { readFile } from 'node:fs/promises';

const DEFAULT_EXTENSION_ID = 'coeeghhijegibfhhodhnjhjffaipckok';
const SCOPE = 'https://www.googleapis.com/auth/chromewebstore';
// Yalnızca testlerde geçersiz kılınır
const API_BASE = process.env.CWS_API_BASE || 'https://chromewebstore.googleapis.com';
const POLL_INTERVAL_MS = Number(process.env.CWS_POLL_INTERVAL_MS || 5000);
const POLL_ATTEMPTS = 24;
// Belgede enum "IN_PROGRESS", upload sayfasında "UPLOAD_IN_PROGRESS" geçiyor; ikisi de kabul edilir
const IN_PROGRESS = new Set(['IN_PROGRESS', 'UPLOAD_IN_PROGRESS']);

class CwsError extends Error {}

function base64url(input) {
  return Buffer.from(input).toString('base64').replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
}

function parseArgs(argv) {
  const opts = { zipPath: null, version: null, statusOnly: false, noPublish: false };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--status') opts.statusOnly = true;
    else if (a === '--no-publish') opts.noPublish = true;
    else if (a === '--version') opts.version = argv[++i];
    else if (!a.startsWith('--')) opts.zipPath = a;
    else throw new CwsError(`Bilinmeyen seçenek: ${a}`);
  }
  if (!opts.statusOnly && !opts.zipPath) {
    throw new CwsError('Paket yolu verilmedi. Kullanım: node publish-extension.mjs <paket.zip> [--version X]');
  }
  return opts;
}

function readConfig() {
  const rawKey = process.env.CWS_SERVICE_ACCOUNT_KEY;
  const publisherId = process.env.CWS_PUBLISHER_ID;
  const extensionId = process.env.CWS_EXTENSION_ID || DEFAULT_EXTENSION_ID;
  if (!rawKey) throw new CwsError('CWS_SERVICE_ACCOUNT_KEY tanımlı değil.');
  if (!publisherId) throw new CwsError('CWS_PUBLISHER_ID tanımlı değil.');

  let key;
  try {
    key = JSON.parse(rawKey);
  } catch {
    throw new CwsError('CWS_SERVICE_ACCOUNT_KEY geçerli bir JSON değil (hizmet hesabı anahtar dosyasının tam içeriği olmalı).');
  }
  if (!key.client_email || !key.private_key) {
    throw new CwsError('Anahtar JSON içinde client_email veya private_key yok.');
  }
  return { key, publisherId, extensionId };
}

/** Hizmet hesabı anahtarıyla imzalı bir JWT oluşturup erişim jetonuyla değiştirir. */
async function getAccessToken(key) {
  const tokenUri = key.token_uri || 'https://oauth2.googleapis.com/token';
  const now = Math.floor(Date.now() / 1000);
  const header = base64url(JSON.stringify({ alg: 'RS256', typ: 'JWT' }));
  const claims = base64url(JSON.stringify({ iss: key.client_email, scope: SCOPE, aud: tokenUri, iat: now, exp: now + 3600 }));
  const signer = createSign('RSA-SHA256');
  signer.update(`${header}.${claims}`);
  const assertion = `${header}.${claims}.${base64url(signer.sign(key.private_key))}`;

  const response = await fetch(tokenUri, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({ grant_type: 'urn:ietf:params:oauth:grant-type:jwt-bearer', assertion })
  });
  const body = await response.json().catch(() => ({}));
  if (!response.ok || !body.access_token) {
    throw new CwsError(`Erişim jetonu alınamadı (HTTP ${response.status}): ${JSON.stringify(body)}`);
  }
  return body.access_token;
}

async function api(token, method, path, { body, contentType } = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    method,
    headers: { Authorization: `Bearer ${token}`, ...(contentType ? { 'Content-Type': contentType } : {}) },
    body
  });
  const text = await response.text();
  let json;
  try { json = text ? JSON.parse(text) : {}; } catch { json = { raw: text }; }
  if (!response.ok) {
    throw new CwsError(`${method} ${path} başarısız (HTTP ${response.status}): ${text}`);
  }
  return json;
}

function versionsOf(revisionStatus) {
  return (revisionStatus?.distributionChannels ?? []).map(c => c.crxVersion).filter(Boolean);
}

export async function run(argv) {
  const opts = parseArgs(argv);
  const { key, publisherId, extensionId } = readConfig();
  const item = `/v2/publishers/${publisherId}/items/${extensionId}`;

  console.log(`Hizmet hesabı: ${key.client_email}`);
  console.log(`Eklenti:       ${extensionId}`);
  const token = await getAccessToken(key);
  const status = await api(token, 'GET', `${item}:fetchStatus`);
  const published = versionsOf(status.publishedItemRevisionStatus);
  const submitted = versionsOf(status.submittedItemRevisionStatus);
  console.log(`Yayında:       ${published.join(', ') || '-'} (${status.publishedItemRevisionStatus?.state ?? '-'})`);
  console.log(`İncelemede:    ${submitted.join(', ') || '-'} (${status.submittedItemRevisionStatus?.state ?? '-'})`);

  if (opts.statusOnly) {
    return { action: 'status', status };
  }

  if (opts.version && (published.includes(opts.version) || submitted.includes(opts.version))) {
    console.log(`Sürüm ${opts.version} mağazada zaten yayında veya incelemede; yükleme atlandı.`);
    return { action: 'skipped' };
  }

  const zip = await readFile(opts.zipPath);
  console.log(`Yükleniyor:    ${opts.zipPath} (${(zip.length / 1024).toFixed(1)} KB)`);
  const upload = await api(token, 'POST', `/upload${item}:upload`, { body: zip, contentType: 'application/zip' });
  let state = upload.uploadState;
  console.log(`Yükleme:       ${state}${upload.crxVersion ? ` (sürüm ${upload.crxVersion})` : ''}`);

  for (let i = 0; IN_PROGRESS.has(state) && i < POLL_ATTEMPTS; i++) {
    await new Promise(r => setTimeout(r, POLL_INTERVAL_MS));
    state = (await api(token, 'GET', `${item}:fetchStatus`)).lastAsyncUploadState ?? state;
    console.log(`  bekleniyor... ${state}`);
  }
  if (state !== 'SUCCEEDED') {
    throw new CwsError(`Yükleme tamamlanamadı (durum: ${state}). Ayrıntı için geliştirici paneline bakın.`);
  }

  if (opts.noPublish) {
    console.log('Yüklendi; --no-publish verildiği için incelemeye gönderilmedi (panelde taslak olarak duruyor).');
    return { action: 'uploaded' };
  }

  const publish = await api(token, 'POST', `${item}:publish`);
  console.log(`İncelemeye gönderildi: ${JSON.stringify(publish)}`);
  return { action: 'published', publish };
}

run(process.argv.slice(2)).catch(err => {
  console.error(`HATA: ${err instanceof CwsError ? err.message : err.stack || err}`);
  process.exit(1);
});
