using PersonalFinanceOfflineTracker.Domain.Models;

namespace PersonalFinanceOfflineTracker.Api.Services;

public interface IJwtTokenService
{
    string CreateAccessToken(User user, DateTime expiresAtUtc);
}
