using System.Net;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Application.Extensions;
using TecChallenge.Shared.Models.Dtos.Requests;
using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Application.V1.Controllers;

[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/games")]
[Produces("application/json")]
public class GameController(
    INotifier notifier,
    IUser appUser,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IGameRepository gameRepository,
    IGameService gameService)
    : MainController(notifier, appUser, httpContextAccessor, webHostEnvironment)
{
    /// <summary>
    /// Get all available games
    /// </summary>
    /// <returns>List of all games</returns>
    /// <response code="200">Returns the list of games</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<GameResponse>>>> GetAllGames()
    {
        var games = (await gameRepository.GetAllAsync()).Select(g => g.MapToDto());
        return CustomResponse(data: games);
    }

    /// <summary>
    /// Get a specific game by its identifier
    /// </summary>
    /// <param name="id">Game identifier</param>
    /// <returns>Game data</returns>
    /// <response code="200">Returns the requested game</response>
    /// <response code="404">Game not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<GameResponse>>> GetGameById(Guid id)
    {
        var game = await gameRepository.GetByIdAsync(id);

        if (game != null) return CustomResponse(data: game.MapToDto());

        NotifyError("Game not found");
        return CustomResponse<GameResponse>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Create a new game
    /// </summary>
    /// <param name="model">Game data</param>
    /// <returns>Created game data</returns>
    /// <response code="201">Game created successfully</response>
    /// <response code="400">Invalid input data</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<GameResponse>>> AddGame(GameAddRequest model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<GameResponse>(ModelState);

        var entity = model.MapToEntity();

        var result = await gameService.AddAsync(entity);

        return result ? CustomResponse(data: entity.MapToDto(), statusCode: HttpStatusCode.Created) : CustomResponse<GameResponse>(statusCode: HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Update an existing game
    /// </summary>
    /// <param name="id">Game identifier</param>
    /// <param name="model">Updated game data</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Game updated successfully</response>
    /// <response code="400">Invalid input data or ID mismatch</response>
    /// <response code="404">Game not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<GameResponse>>> UpdateGame(Guid id, GameUpdateRequest model)
    {
        if (id != model.Id)
        {
            NotifyError("The ids entered are not the same");
            return CustomResponse<GameResponse>();
        }

        if (!ModelState.IsValid) return CustomModelStateResponse<GameResponse>(ModelState);

        var result = await gameService.UpdateAsync(id, model.MapToEntity());

        if (result != null)
            return !result.Value
                ? CustomResponse<GameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<GameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<GameResponse>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Delete a game
    /// </summary>
    /// <param name="id">Game identifier</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Game deleted successfully</response>
    /// <response code="404">Game not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<GameResponse>>> DeleteGame(Guid id)
    {
        var result = await gameService.DeleteAsync(id);

        if (result != null)
            return !result.Value
                ? CustomResponse<GameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<GameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<GameResponse>(statusCode: HttpStatusCode.NotFound);
    }
}