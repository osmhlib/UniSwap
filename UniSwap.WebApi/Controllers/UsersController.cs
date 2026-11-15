using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.Users;
using UniSwap.WebApi.Auth;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UsersController(Accounts accounts) : ControllerBase
{
    [HttpGet]
    public Task<ProfileResponse> Profile(CancellationToken ct) => accounts.GetProfileAsync(User.UserId(), ct);

    [HttpPut]
    public Task<ProfileResponse> Update(UpdateProfileRequest request, CancellationToken ct) =>
        accounts.UpdateProfileAsync(User.UserId(), request.Name, ct);
}
