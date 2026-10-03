export interface Envelope { sessionId: string; requestId: string; nonce: string; ciphertext: string; tag: string; }
interface Session { sessionId: string; publicKey: string; salt: string; expiresAt: string; }
const encode = (value: string) => new TextEncoder().encode(value);
const base64 = (bytes: Uint8Array) => btoa(String.fromCharCode(...bytes));
const bytes = (value: string) => Uint8Array.from(atob(value), c => c.charCodeAt(0));

// Pure Web Crypto transport: usable by Angular and the Node interoperability checks.
export class EncryptedTransport {
  private session?: Session;
  private requestKey?: CryptoKey;
  private responseKey?: CryptoKey;
  private pendingSession?: Promise<void>;
  constructor(private readonly baseUrl = '') {}

  async connect(): Promise<void> {
    if (this.session && Date.parse(this.session.expiresAt) > Date.now() + 5000) return;
    if (!this.pendingSession) this.pendingSession = this.createSession().finally(() => { this.pendingSession = undefined; });
    await this.pendingSession;
  }
  private async createSession() {
    const pair = await crypto.subtle.generateKey({ name: 'ECDH', namedCurve: 'P-256' }, false, ['deriveBits']);
    const publicKey = base64(new Uint8Array(await crypto.subtle.exportKey('spki', pair.publicKey)));
    const response = await fetch(this.baseUrl + '/api/crypto/session', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ publicKey }), signal: AbortSignal.timeout(15000)
    });
    if (!response.ok) throw new Error(`Session setup failed (HTTP ${response.status}).`);
    const session: Session = await response.json();
    const peer = await crypto.subtle.importKey('spki', bytes(session.publicKey), { name: 'ECDH', namedCurve: 'P-256' }, false, []);
    const secret = new Uint8Array(await crypto.subtle.deriveBits({ name: 'ECDH', public: peer }, pair.privateKey, 256));
    try {
      const material = await crypto.subtle.importKey('raw', secret, 'HKDF', false, ['deriveKey']);
      const derive = (direction: string) => crypto.subtle.deriveKey({
        name: 'HKDF', hash: 'SHA-256', salt: bytes(session.salt), info: encode(`aes-gcm-demo:v1:${direction}:${session.sessionId}`)
      }, material, { name: 'AES-GCM', length: 256 }, false, direction === 'request' ? ['encrypt'] : ['decrypt']);
      this.requestKey = await derive('request');
      this.responseKey = await derive('response');
      this.session = session;
    } finally { secret.fill(0); }
  }
  private aad(envelope: Envelope, path: string, direction: string) {
    return encode(`v1|${envelope.sessionId}|${envelope.requestId}|POST|${path}|${direction}`);
  }
  async encrypt(path: string, payload: unknown): Promise<Envelope> {
    await this.connect();
    const nonce = crypto.getRandomValues(new Uint8Array(12));
    const envelope: Envelope = { sessionId: this.session!.sessionId, requestId: crypto.randomUUID(), nonce: base64(nonce), ciphertext: '', tag: '' };
    const plaintext = encode(JSON.stringify(payload));
    if (plaintext.length > 16000) throw new Error('Payload exceeds demo limit.');
    const combined = new Uint8Array(await crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce, additionalData: this.aad(envelope, path, 'request'), tagLength: 128 }, this.requestKey!, plaintext));
    envelope.ciphertext = base64(combined.slice(0, -16));
    envelope.tag = base64(combined.slice(-16));
    return envelope;
  }
  async decrypt(path: string, request: Envelope, response: Envelope): Promise<unknown> {
    if (response.sessionId !== request.sessionId || response.requestId !== request.requestId) throw new Error('Response does not match the request.');
    const ciphertext = bytes(response.ciphertext);
    const tag = bytes(response.tag);
    const nonce = bytes(response.nonce);
    if (nonce.length !== 12 || tag.length !== 16) throw new Error('Invalid response envelope.');
    const combined = new Uint8Array(ciphertext.length + tag.length);
    combined.set(ciphertext); combined.set(tag, ciphertext.length);
    const plaintext = await crypto.subtle.decrypt({ name: 'AES-GCM', iv: nonce, additionalData: this.aad(response, path, 'response'), tagLength: 128 }, this.responseKey!, combined);
    return JSON.parse(new TextDecoder().decode(plaintext));
  }
  async post(path: string, payload: unknown) {
    const request = await this.encrypt(path, payload);
    const response = await fetch(this.baseUrl + path, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(request), signal: AbortSignal.timeout(15000)
    });
    if (!response.ok) throw new Error(`Encrypted request rejected (HTTP ${response.status}).`);
    const encrypted: Envelope = await response.json();
    return { request, response: encrypted, plaintext: await this.decrypt(path, request, encrypted) };
  }
}
