using Demo.Api.Models;
using Demo.Api.Repositories;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
IUserRepository users = new InMemoryUserRepository();
var user = new UserEntity("id", "Test", "test@example.com", "hash");
var adds = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => users.TryAdd(user))));
Check(adds.Count(x => x == AddUserResult.Added) == 1, "Concurrent duplicate registration must create one user.");
Check(adds.Count(x => x == AddUserResult.Duplicate) == 31, "Duplicate writes must not overwrite.");
var snapshot = users.FindByEmail(user.Email)!;
users.Update(snapshot with { Failures = 3 });
Check(snapshot.Failures == 0 && users.FindByEmail(user.Email)!.Failures == 3, "Snapshots must be immutable; updates must persist.");
for (var i = 1; i < 1000; i++) Check(users.TryAdd(user with { Email = $"{i}@example.com" }) == AddUserResult.Added, "Capacity reached too early.");
Check(users.TryAdd(user with { Email = "overflow@example.com" }) == AddUserResult.CapacityReached, "User capacity must be bounded.");
Check(users.TryAdd(user) == AddUserResult.Duplicate, "Duplicates at capacity must still be recognized.");
try { users.Update(user with { Email = "missing@example.com" }); throw new Exception("Missing user update unexpectedly succeeded."); }
catch (InvalidOperationException) { }
ILoginSessionRepository logins = new InMemoryLoginSessionRepository();
Check(logins.TryAdd("expired", new(user.Email, DateTimeOffset.UtcNow.AddSeconds(-1))), "Expired fixture setup failed.");
Check(logins.FindByTokenHash("expired") is null, "Expired sessions must not authenticate.");
var future = new LoginSession(user.Email, DateTimeOffset.UtcNow.AddMinutes(5));
Check(logins.TryAdd("valid", future), "Valid login must persist.");
Check(!logins.TryAdd("valid", future), "Token hash duplicate must not overwrite.");
Check(logins.FindByTokenHash("valid") == future, "Valid session lookup failed.");
logins.Remove("valid");
Check(logins.FindByTokenHash("valid") is null, "Revocation must remove session.");
for (var i = 0; i < 1000; i++) Check(logins.TryAdd(i.ToString(), future), "Session capacity reached too early.");
Check(!logins.TryAdd("overflow", future), "Session capacity must be bounded.");
Console.WriteLine("PASS: repository contracts, atomic duplicate writes, immutable snapshots, updates, capacity, expired sessions and revocation.");
