# Architecture and encryption plan

The gateway will forward encrypted envelopes without possessing encryption keys. Angular encrypts before sending; the API decrypts and handles the business operation. The API encrypts its response; Angular decrypts it.

Session bootstrap uses an ephemeral browser key and API key agreement over HTTPS. AES-256-GCM session keys stay in browser memory and API memory, expire, and are never hardcoded in frontend configuration. Session establishment is transport setup, not user authentication.

Each message uses a fresh 12-byte nonce and a 16-byte authentication tag. The JSON envelope will use Base64 session metadata, ciphertext, nonce and tag. Authenticated metadata will bind the session, HTTP operation, request identifier and direction. Responses are bound to the originating request, and replayed request identifiers are rejected. The Web Crypto combined ciphertext/tag output will be split for .NET AesGcm interoperability.

Login/register payloads and their application responses will be encrypted. Session setup, health and failures that happen before a valid encryption session exists require explicitly defined plaintext protocol responses.

Local HTTP on localhost is a development setup. Deployments require HTTPS, a supported patched runtime, rate limiting and persistent identity storage. Browser encryption cannot protect against malicious JavaScript executing inside that browser.
