using System.Net;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Application.Extensions;
using TecChallenge.Shared.Models.Dtos;
using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Application.V1.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/user-libraries")]
[Produces("application/json")]
public class UserLibraryController(
    INotifier notifier,
    IUser appUser,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IUserLibraryRepository userLibraryRepository)
    : MainController(notifier, appUser, httpContextAccessor, webHostEnvironment)
{
    /// <summary>
    /// Get a user's game library
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>The user's library containing their game collection</returns>
    /// <response code="200">Returns the user's game library</response>
    /// <response code="404">User library not found</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<UserLibraryResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserLibraryResponse>>> GetUserLibrary(Guid userId)
    {
        var userLibrary = await userLibraryRepository.FirstOrDefaultAsync(
            x => x.UserId == userId,
            false,
            x => x.Items);

        if (userLibrary != null) return CustomResponse(data: userLibrary.MapToDto());

        NotifyError("User library not found");

        return CustomResponse<UserLibraryResponse>(statusCode: HttpStatusCode.NotFound);
    }
}