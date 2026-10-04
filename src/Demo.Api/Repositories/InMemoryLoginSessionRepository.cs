using Demo.Api.Models;
namespace Demo.Api.Repositories;

public sealed class InMemoryLoginSessionRepository : ILoginSessionRepository
{
    private readonly Dictionary<string, LoginSession> sessions = new(StringComparer.Ordinal);
    private readonly object gate = new();
    public LoginSession? FindByTokenHash(string tokenHash)
    {
        lock (gate)
        {
            if (!sessions.TryGetValue(tokenHash, out var session)) return null;
            if (session.Expires > DateTimeOffset.UtcNow) return session;
            sessions.Remove(tokenHash);
            return null;
        }
    }
    public bool TryAdd(string tokenHash, LoginSession session)
    {
        lock (gate)
        {
            foreach (var key in sessions.Where(x => x.Value.Expires <= DateTimeOffset.UtcNow).Select(x => x.Key).ToArray()) sessions.Remove(key);
            return sessions.Count < 1000 && sessions.TryAdd(tokenHash, session);
        }
    }
    public void Remove(string tokenHash) { lock (gate) sessions.Remove(tokenHash); }
}
