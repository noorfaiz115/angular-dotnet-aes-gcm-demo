# Implemented architecture

Angular encrypts before sending; the API decrypts and handles the business operation. The API encrypts its response; Angular decrypts it. The gateway forwards encrypted envelopes without holding session keys.

Module 2 implements ephemeral P-256 ECDH and HKDF-SHA256 with separate AES-256-GCM keys for each direction. Session keys live in memory, expire after ten minutes and are never hardcoded in frontend configuration. Session establishment is transport setup, not user authentication.

Each message uses a fresh 12-byte nonce and a 16-byte authentication tag. Base64 carries ciphertext, nonce and tag in JSON. Authenticated metadata binds the session, HTTP operation, request identifier and direction. Responses bind to the originating request, and replayed request IDs are rejected.

Module 3 applies this transport to registration, login, protected profile and logout. Passwords use the ASP.NET Core Identity salted password hasher. Expiring opaque login tokens stay in browser memory and travel inside encrypted payloads; the API stores token hashes. Health, session setup and failures before a trusted encryption context exists use plaintext protocol responses.

Local HTTP on localhost is a development setup. Deployment requires authenticated HTTPS, supported patched runtimes, rate limiting and persistent identity storage. Browser encryption cannot protect against malicious JavaScript executing inside the page. This implementation stores sessions in one API process; scaling out requires a deliberate shared-state strategy.

Backend HTTP routes now live in Controllers; AuthService implements identity rules using IUserRepository and ILoginSessionRepository. In-memory implementations preserve the existing demo state and limits. See module-4-repository-pattern.md for the current file map and future database changes.
