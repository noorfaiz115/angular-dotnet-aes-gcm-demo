# Module 2: encrypted round trip

## Flow

1. Browser generates an ephemeral P-256 key pair. It sends only its public key to POST /api/crypto/session.
2. Gateway forwards that request to the API. API generates its own ephemeral key pair, derives the shared secret, and returns its public key, random HKDF salt, session ID and expiry.
3. Browser and API independently apply HKDF-SHA256 to the same shared secret. Different info labels produce separate request and response AES-256 keys. Neither AES key is transmitted.
4. Browser serializes JSON and encrypts with AES-GCM using a fresh random 12-byte nonce and a 128-bit tag. It separates Web Crypto's combined ciphertext/tag into the envelope fields .NET expects.
5. Gateway forwards the opaque JSON envelope. API authenticates and decrypts, then processes the echo payload.
6. API encrypts its response with the response key and a fresh nonce. Angular checks the response request ID, authenticates the tag, and decrypts.

Public-key bootstrap must use authenticated HTTPS in deployment. Without it, an intermediary can substitute keys. The session ID identifies transport state; it does not authenticate a logged-in user.

## Envelope

```json
{
  "sessionId": "32-character session identifier",
  "requestId": "UUID",
  "nonce": "Base64 of 12 bytes",
  "ciphertext": "Base64 encrypted JSON",
  "tag": "Base64 of 16 bytes"
}
```

Session and request IDs are visible metadata. Base64 encodes bytes; it does not encrypt them.

Authenticated additional data is the exact UTF-8 string:

`v1|sessionId|requestId|POST|/api/secure/echo|request`

The response uses `response` for the final field. Binding the path, method, ID and direction prevents accepting an envelope in the wrong context.

## Limits and errors

- Session expires after 10 minutes. Browser renews before sending when near expiry.
- At most 1,000 active sessions and 1,000 accepted requests per session.
- Bootstrap has a global 30-per-minute limiter for this single-process demo.
- HTTP bodies are limited to 32 KiB; client plaintext is limited to 16,000 bytes.
- Request IDs enter the replay set only after successful authentication. Replays return HTTP 409.
- Invalid tags or malformed envelopes return HTTP 400. Unknown/expired sessions return HTTP 409.
- These transport failures are plaintext because the envelope has not established a trusted context. Successfully decrypted but invalid JSON produces an encrypted application error.
- Keys and replay state remain in memory. Restarting API invalidates existing sessions. Refresh the page to establish a new one. No transparent retry occurs for uncertain delivery.

## Files to read

- src/client/src/app/encrypted-transport.ts: Web Crypto operations and envelope splitting.
- src/Demo.Api/Transport/SessionStore.cs: session derivation, AES-GCM and replay protection.
- src/Demo.Gateway/Program.cs: forwarding without key access.
- scripts/verify-transport.mjs: executable interoperability checks.

Standards/API references: https://www.w3.org/TR/WebCryptoAPI/ and https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm
