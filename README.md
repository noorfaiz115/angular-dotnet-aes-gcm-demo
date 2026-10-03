# AES-GCM learning project

Angular client -> .NET gateway -> .NET API.

## Module 1: application foundation

- Angular 21 standalone client (Node 20.19+).
- .NET 8 API at http://localhost:5200.
- .NET 8 gateway at http://localhost:5100.
- Angular dev server at http://localhost:4200 forwards /api to the gateway.
- Browser health check travels through all three applications.

Run in three terminals from the repository root:

```powershell
dotnet run --project src/Demo.Api
dotnet run --project src/Demo.Gateway
cd src/client
npm start
```

Open http://localhost:4200. Click Check connection.

Validation:

```powershell
dotnet build AesGcmDemo.sln
cd src/client
npm run build
```

## Next modules

2. AES-GCM session and encrypted request/response contract.
3. Login and registration with hashed passwords.
4. Automated interoperability, tamper and replay checks.

See docs/architecture.md for the encryption design. Module 1 contains no encryption or authentication yet.
