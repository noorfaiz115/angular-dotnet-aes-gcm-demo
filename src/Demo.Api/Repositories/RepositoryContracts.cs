using Demo.Api.Models;
namespace Demo.Api.Repositories;

public enum AddUserResult { Added, Duplicate, CapacityReached }
public interface IUserRepository
{
    UserEntity? FindByEmail(string email);
    AddUserResult TryAdd(UserEntity user);
    void Update(UserEntity user);
}
public interface ILoginSessionRepository
{
    LoginSession? FindByTokenHash(string tokenHash);
    bool TryAdd(string tokenHash, LoginSession session);
    void Remove(string tokenHash);
}
