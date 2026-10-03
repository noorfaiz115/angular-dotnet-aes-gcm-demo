# Module 3: encrypted login and registration

## Try it

1. Start all three applications as described in README.md.
2. Open http://localhost:4200/register. Create an account with a name, email and a password of 12–128 characters.
3. Open /login and log in. Registration creates the account; it does not automatically log you in.
4. The /profile page calls the protected API to verify your login. Click **Verify profile with API** to repeat the check.
5. Click **Log out**. The API revokes the token; subsequent profile requests with it are rejected.
6. The /lab page still demonstrates encrypted echo payloads. Authentication credentials and tokens never enter those debug panels.

## Transport and identity are separate

A crypto session lets Angular and .NET encrypt/decrypt. It does not identify a user. Login verifies a salted password hash and issues an independent cryptographically random 256-bit opaque login token.

The token travels inside the encrypted login response, then inside encrypted profile/logout request bodies. It is held in the Angular authentication service memory, with no localStorage, sessionStorage or cookies. The API stores only its SHA-256 lookup hash and expires the login after 30 minutes. Logout removes that record.

The Angular route guard is a navigation convenience. The API independently verifies the login token before returning a profile. Having an encryption session alone grants no identity access.

## Encrypted endpoint contracts

All routes below use POST with the Module 2 encrypted envelope:

| Route | Decrypted request | Decrypted response |
| --- | --- | --- |
| /api/auth/register | name, email, password | ok, message, public user |
| /api/auth/login | email, password | ok, message, public user, token |
| /api/auth/me | token | ok, message, public user |
| /api/auth/logout | token | ok, message |

Successfully authenticated transport envelopes return HTTP 200 with an encrypted application result, even for an invalid password or missing login token. Check the decrypted `ok` field. Invalid transport returns the Module 2 plaintext protocol error instead.

The API uses ASP.NET Core Identity PasswordHasher, which creates salted password hashes and verifies them. Email addresses normalize to lowercase. Names and emails are bounded, passwords are limited to 12–128 characters, and duplicate email registration is atomic. Public profiles contain only id, name and email.

## Demo limits

- Users and login sessions live in memory, each bounded to 1,000 records. API restart resets accounts and active logins.
- Browser refresh clears its login token. Log in again; the previous token expires on the server.
- Register/login share a global 60-attempt-per-minute budget. Budget errors are encrypted application results.
- Five failed passwords temporarily lock an existing account for one minute. Login failure messages do not distinguish missing users, incorrect passwords or locked accounts.
- Identity work and crypto operations are serialized for simplicity in this single-process lab.
- Localhost HTTP is for development. Production requires authenticated HTTPS and a deliberate persistent/distributed identity design.

## Validation

`node scripts/verify-auth.mjs` exercises the actual Web Crypto transport through the gateway/API. It covers encrypted registration/login/profile/logout, validation, case normalization, duplicate registration, incorrect credentials, missing/forged tokens, tampered envelopes, revocation, lockout and concurrent registration. `scripts/verify-transport.mjs` covers encryption regressions.

Both .NET and Angular builds pass. Automated browser interaction is not verified: the browser-control runtime could not start on this host. HTTP delivery and the Angular dev proxy authentication flow are verified.

API reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1
