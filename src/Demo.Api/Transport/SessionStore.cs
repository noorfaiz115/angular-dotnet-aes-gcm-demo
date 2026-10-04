using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Demo.Api.Transport;

public record SessionRequest(string PublicKey);
public record SessionResponse(string SessionId, string PublicKey, string Salt, DateTimeOffset ExpiresAt);
public record Envelope(string SessionId, string RequestId, string Nonce, string Ciphertext, string Tag);

public sealed class SessionStore : IEncryptedSessionService, IDisposable
{
    private sealed class Session(byte[] requestKey, byte[] responseKey, DateTimeOffset expires)
    {
        public byte[] RequestKey = requestKey;
        public byte[] ResponseKey = responseKey;
        public DateTimeOffset Expires = expires;
        public HashSet<string> Requests = new();
    }
    private readonly Dictionary<string, Session> sessions = new();
    private readonly object gate = new();
    public SessionResponse Create(SessionRequest request)
    {
        lock (gate)
        {
            foreach (var id in sessions.Where(x => x.Value.Expires <= DateTimeOffset.UtcNow).Select(x => x.Key).ToArray())
            {
                Clear(sessions[id]);
                sessions.Remove(id);
            }
            if (sessions.Count >= 1000) throw new InvalidOperationException("Session capacity reached.");
            if (request.PublicKey is null || request.PublicKey.Length > 256) throw new CryptographicException();
            using var peer = ECDiffieHellman.Create();
            var publicBytes = Convert.FromBase64String(request.PublicKey);
            peer.ImportSubjectPublicKeyInfo(publicBytes, out var read);
            if (read != publicBytes.Length || peer.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7") throw new CryptographicException();
            using var server = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var secret = server.DeriveRawSecretAgreement(peer.PublicKey);
            try
            {
                var salt = RandomNumberGenerator.GetBytes(32);
                var id = Guid.NewGuid().ToString("N");
                var expires = DateTimeOffset.UtcNow.AddMinutes(10);
                var requestKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, 32, salt, Encoding.UTF8.GetBytes("aes-gcm-demo:v1:request:" + id));
                var responseKey = HKDF.DeriveKey(HashAlgorithmName.SHA256, secret, 32, salt, Encoding.UTF8.GetBytes("aes-gcm-demo:v1:response:" + id));
                sessions.Add(id, new Session(requestKey, responseKey, expires));
                return new(id, Convert.ToBase64String(server.ExportSubjectPublicKeyInfo()), Convert.ToBase64String(salt), expires);
            }
            finally { CryptographicOperations.ZeroMemory(secret); }
        }
    }
    public Envelope Echo(Envelope envelope, string path) => Process(envelope, path, payload => new { message = "Encrypted round trip successful", received = payload.Clone() });
    public Envelope Process(Envelope envelope, string path, Func<JsonElement, object> handler)
    {
        lock (gate)
        {
            if (envelope.SessionId is null || !sessions.TryGetValue(envelope.SessionId, out var session) || session.Expires <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Session expired or unknown.");
            if (envelope.RequestId is null || !Guid.TryParseExact(envelope.RequestId, "D", out _)) throw new CryptographicException();
            if (session.Requests.Contains(envelope.RequestId)) throw new InvalidOperationException("Replay rejected.");
            if (session.Requests.Count >= 1000) throw new InvalidOperationException("Create a new session.");
            if (envelope.Nonce is null || envelope.Tag is null || envelope.Ciphertext is null || envelope.Ciphertext.Length > 24000) throw new CryptographicException();
            var nonce = Convert.FromBase64String(envelope.Nonce);
            var tag = Convert.FromBase64String(envelope.Tag);
            var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
            if (nonce.Length != 12 || tag.Length != 16) throw new CryptographicException();
            var plaintext = new byte[ciphertext.Length];
            try
            {
                using var decryptor = new AesGcm(session.RequestKey, 16);
                decryptor.Decrypt(nonce, ciphertext, tag, plaintext, Aad(envelope, path, "request"));
                session.Requests.Add(envelope.RequestId);
                object result;
                try
                {
                    using var doc = JsonDocument.Parse(plaintext);
                    result = handler(doc.RootElement);
                }
                catch (JsonException) { result = new { error = "Payload must be valid JSON." }; }
                var responseBytes = JsonSerializer.SerializeToUtf8Bytes(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                try
                {
                    var responseNonce = RandomNumberGenerator.GetBytes(12);
                    var responseCiphertext = new byte[responseBytes.Length];
                    var responseTag = new byte[16];
                    using var encryptor = new AesGcm(session.ResponseKey, 16);
                    encryptor.Encrypt(responseNonce, responseBytes, responseCiphertext, responseTag, Aad(envelope, path, "response"));
                    return new(envelope.SessionId, envelope.RequestId, Convert.ToBase64String(responseNonce), Convert.ToBase64String(responseCiphertext), Convert.ToBase64String(responseTag));
                }
                finally { CryptographicOperations.ZeroMemory(responseBytes); }
            }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
        }
    }
    private static byte[] Aad(Envelope e, string path, string direction) => Encoding.UTF8.GetBytes($"v1|{e.SessionId}|{e.RequestId}|POST|{path}|{direction}");
    private static void Clear(Session s) { CryptographicOperations.ZeroMemory(s.RequestKey); CryptographicOperations.ZeroMemory(s.ResponseKey); }
    public void Dispose() { lock (gate) { foreach (var session in sessions.Values) Clear(session); sessions.Clear(); } }
}
