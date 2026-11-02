using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;

namespace NetProject.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> AuthenticateAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class AuthService : IAuthService
{
    private readonly WarehouseDbContext _context;

    public AuthService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<LoginResponseDto?> AuthenticateAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var username = request.Username.Trim();
        var user = await _context.Users
            .AsNoTracking()
            .Include(candidate => candidate.Role)
            .FirstOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);

        if (user is null || !user.IsActive || !SecurityHelper.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        return new LoginResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role.RoleName
        };
    }
}
