using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.Users;
using UniSwap.WebApi.Auth;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(Accounts accounts) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProfileResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created,
            await accounts.RegisterAsync(request.Name, request.Email, request.Password, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var session = await accounts.LoginAsync(request.Email, request.Password, ct);
        return session is null ? Unauthorized() : Ok(session);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await accounts.LogoutAsync(User.FindFirstValue(AccountIdentity.SessionClaim)!, ct);
        return NoContent();
    }
}
