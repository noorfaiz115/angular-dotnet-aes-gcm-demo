using System.Text.Json;
namespace Demo.Api.Transport;

public interface IEncryptedSessionService
{
    SessionResponse Create(SessionRequest request);
    Envelope Echo(Envelope envelope, string path);
    Envelope Process(Envelope envelope, string path, Func<JsonElement, object> handler);
}
