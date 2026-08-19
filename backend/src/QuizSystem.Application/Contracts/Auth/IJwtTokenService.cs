using QuizSystem.Application.DTOs;
using QuizSystem.Domain.Entities;

namespace QuizSystem.Application.Contracts.Auth;
public interface IJwtTokenService
{
    (string token, DateTime expiresAtUtc) GenerateToken(AppUser user);
}
