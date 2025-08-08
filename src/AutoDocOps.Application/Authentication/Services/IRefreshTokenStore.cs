namespace AutoDocOps.Application.Authentication.Services;

public interface IRefreshTokenStore
{
    Task<bool> IsValidRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiration, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}
