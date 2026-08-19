using QuizSystem.Application.DTOs;

namespace QuizSystem.Application.Contracts.Auth;
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
