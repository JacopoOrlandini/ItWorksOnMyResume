using IWOMR.Application.Common.Interfaces;

namespace IWOMR.Infrastructure.Auth;

public class PasswordService : IPasswordService
{
    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);

    public bool Verify(string plainPassword, string hash) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}
