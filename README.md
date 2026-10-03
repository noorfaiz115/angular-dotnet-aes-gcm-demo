# AES-GCM learning project

Angular client -> .NET gateway -> .NET API. A module-by-module learning demo.

## Completed modules

1. Angular 21 standalone client, .NET 8 gateway/API, health check.
2. AES-256-GCM encrypted echo request/response, ephemeral P-256/HKDF session, tamper and replay checks.
3. Encrypted login/register, hashed passwords, protected profile and token-revoking logout.

## Run locally

Prerequisites: Node 20.19+ and .NET SDK 8 or newer with the .NET 8 runtime.

Install client dependencies once: `cd src/client` then `npm ci`.

Run each application in its own terminal from the repository root:

```powershell
dotnet run --project src/Demo.Api
```

```powershell
dotnet run --project src/Demo.Gateway
```

```powershell
cd src/client
npm start
```

Open http://localhost:4200/register to create an account, then log in and verify your profile. Open http://localhost:4200/lab. Enter a message and click **Send encrypted message**. The screen displays encrypted request/response envelopes and the response decrypted inside Angular. These debug panels use synthetic echo messages. Authentication credentials and tokens do not enter them.

## Verify

```powershell
dotnet build AesGcmDemo.sln
cd src/client
npm run build
```

With API and gateway running, from the repository root:

```powershell
node scripts/verify-transport.mjs
node scripts/verify-auth.mjs
```

To also exercise the Angular dev proxy (with Angular running):

```powershell
$env:DEMO_URL = 'http://localhost:4200'
node scripts/verify-transport.mjs
node scripts/verify-auth.mjs
Remove-Item Env:DEMO_URL
```

The script executes the actual client transport using Node Web Crypto against .NET. It checks Unicode/nested JSON round trips, replay rejection, tampered request and response tags, response request-ID binding, route binding, nonce length, unknown sessions and concurrent requests. It does not automate browser UI interactions.

## Authentication

Login/register are implemented. Users reset on API restart; browser refresh clears its memory-only login token. See [Module 3](docs/module-3.md) for the flow and limits.

See [architecture](docs/architecture.md) and [Module 2 walkthrough](docs/module-2.md). This is a single-process learning demo, not a deployed identity system. HTTPS is required outside localhost. Health, session bootstrap, and protocol failures remain plaintext. Runtime patching, persistence, distributed session handling and deployment hardening are separate work.
