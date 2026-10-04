using Demo.Api.Models;
namespace Demo.Api.Repositories;

// Immutable snapshots prevent callers from modifying stored state without Update.
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<string, UserEntity> users = new(StringComparer.Ordinal);
    private readonly object gate = new();
    public UserEntity? FindByEmail(string email) { lock (gate) return users.GetValueOrDefault(email); }
    public AddUserResult TryAdd(UserEntity user)
    {
        lock (gate)
        {
            if (users.ContainsKey(user.Email)) return AddUserResult.Duplicate;
            if (users.Count >= 1000) return AddUserResult.CapacityReached;
            users.Add(user.Email, user);
            return AddUserResult.Added;
        }
    }
    public void Update(UserEntity user)
    {
        lock (gate)
        {
            if (!users.ContainsKey(user.Email)) throw new InvalidOperationException("User does not exist.");
            users[user.Email] = user;
        }
    }
}
