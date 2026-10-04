using Demo.Api.Auth;
using System.Text.Json;
namespace Demo.Api.Services;

public interface IAuthService
{
    AuthResult Handle(string operation, JsonElement payload);
}
