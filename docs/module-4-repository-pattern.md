# Module 4: repository pattern (simple Hinglish)

## Ab flow kya hai?

Angular -> Gateway endpoint -> ApiForwarder -> API controller -> encrypted session service -> AuthService -> repository.

Response isi flow se encrypted form mein wapas aati hai. Angular ke routes, JSON fields aur encryption algorithm same hain.

## Har layer ka kaam

| Layer | Files | Responsibility |
| --- | --- | --- |
| Startup | Demo.Api/Program.cs | App create, middleware, controller mapping. |
| DI configuration | Demo.Api/Configuration/ServiceRegistration.cs | Interfaces ko implementations se connect karna. |
| HTTP controllers | Demo.Api/Controllers/AuthController.cs, CryptoController.cs, HealthController.cs | Route, HTTP result aur encrypted transport errors. |
| Business service | Demo.Api/Services/IAuthService.cs, AuthService.cs | Input validation, password hashing/verification, login rules, lockout, token creation. |
| Data contracts | Demo.Api/Auth/AuthContracts.cs | Request/response DTOs. |
| Stored entities | Demo.Api/Models/IdentityEntities.cs | Immutable user aur login data. |
| Repository contracts | Demo.Api/Repositories/RepositoryContracts.cs | IUserRepository aur ILoginSessionRepository. |
| Data implementations | InMemoryUserRepository.cs, InMemoryLoginSessionRepository.cs | User/token-hash lookup, insert/update/remove, expiry cleanup aur capacity. |
| Crypto service | Demo.Api/Transport/IEncryptedSessionService.cs, SessionStore.cs | ECDH/HKDF keys, AES-GCM, crypto-session expiry aur replay protection. |
| Gateway routes | Demo.Gateway/Endpoints/GatewayEndpoints.cs | Explicit route allowlist. |
| Gateway forwarding | Demo.Gateway/Services/ApiForwarder.cs | HTTP request/response forwarding, timeout handling. |

Table paths are relative to src unless otherwise noted.

## Repository pattern ka practical benefit

AuthService ko Dictionary ka pata nahi hai. Wo IUserRepository aur ILoginSessionRepository par depend karti hai. Isliye data storage alag change ho sakti hai aur controller ko storage details nahi chahiye.

IUserRepository:

- FindByEmail: user ka immutable snapshot.
- TryAdd: atomically Added, Duplicate ya CapacityReached return karta hai.
- Update: existing user ka updated snapshot save karta hai, including failed-login count.

ILoginSessionRepository:

- FindByTokenHash: valid, unexpired login; expired session remove hoti hai.
- TryAdd: expired records clean karke bounded insert karta hai.
- Remove: logout par token revoke karta hai.

Repository passwords hash nahi karti. AuthService hashing karke entity repository ko deti hai. Login tokens ke SHA-256 hashes store hote hain; raw token encrypted response mein client ko milta hai.

## Encryption storage alag kyun hai?

SessionStore specialized crypto-session service hai. Usmein key material aur replay state saath locked/cleaned hote hain. User repository aur crypto sessions ko mix nahi kiya hai. Public IEncryptedSessionService controller ko implementation se separate rakhta hai.

## Lifetimes aur concurrency

In-memory repositories, AuthService aur crypto service singleton hain. Isse requests ke beech users, login tokens, global attempt limits aur locks shared rehte hain. Repository locks atomic duplicate insertion/capacity/revocation protect karte hain. User records immutable hain, so FindByEmail se mila snapshot silently stored state mutate nahi kar sakta.

AuthService login/lockout operations serialize karti hai; this remains a single-process learning design. API restart par accounts reset honge.

## Future database kaise add hogi?

1. IUserRepository aur ILoginSessionRepository implement karne wali database classes banao.
2. Normalized email par unique constraint, token-hash lookup, expiry aur capacity policies preserve karo.
3. ServiceRegistration mein DI registrations replace karo.
4. EF Core DbContext use karo toh scoped lifetimes aur async contracts/service calls adopt karo; scoped repository ko current singleton AuthService mein directly inject mat karo.
5. Concurrent failed-login updates/registration ke liye database transactions or concurrency control implement karo. Process-local locks multiple API instances ko coordinate nahi karte.
6. Existing contract + encrypted auth tests new storage ke against run karo.

## Naya feature kahan add hoga?

New HTTP route: controller. Business validation/authorization: service. Data reads/writes: repository contract + implementation. Gateway path: GatewayEndpoints. Angular encrypted call: feature service. Program.cs mein feature logic add karne ki zarurat nahi.

## Verify

From repository root:

```powershell
dotnet build AesGcmDemo.sln
dotnet run --project tests/Demo.RepositoryChecks
# API and gateway running:
node scripts/verify-transport.mjs
node scripts/verify-auth.mjs
```

Repository checks: concurrent duplicate writes, immutable snapshots, updates, capacity, expired logins and token removal. Integration checks: encryption, replay, login/register/profile/logout, lockout and duplicate registration. Authentication was also checked through Angular's /api dev proxy.

The existing PDF guide is a historical walkthrough for baseline d3a2b38. Use this document for the current backend file map.
