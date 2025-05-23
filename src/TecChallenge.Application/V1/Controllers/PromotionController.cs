using System.Net;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Application.Extensions;
using TecChallenge.Shared.Models.Dtos.Requests;
using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Application.V1.Controllers;

[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/promotions")]
[Produces("application/json")]
public class PromotionController(
    INotifier notifier,
    IUser appUser,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    IPromotionRepository promotionRepository,
    IPromotionService promotionService)
    : MainController(notifier, appUser, httpContextAccessor, webHostEnvironment)
{
    /// <summary>
    /// Retrieves all active promotions
    /// </summary>
    /// <returns>List of promotion objects</returns>
    /// <response code="200">Returns the promotions list</response>
    [HttpGet]
    [ProducesResponseType(typeof(Root<IEnumerable<PromotionResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Root<IEnumerable<PromotionResponse>>>> GetAllPromotions()
    {
        var promotions = (await promotionRepository.GetAllAsync())
            .Select(g => g.MapToDto());

        return CustomResponse(data: promotions);
    }

    /// <summary>
    /// Gets a specific promotion by ID
    /// </summary>
    /// <param name="id">Promotion unique identifier</param>
    /// <returns>Promotion details</returns>
    /// <response code="200">Promotion found and returned</response>
    /// <response code="404">Promotion not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Root<PromotionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<PromotionResponse>>> GetPromotionById(Guid id)
    {
        var promotion = await promotionRepository.FirstOrDefaultAsync(x => x.Id == id, includes: x => x.GamesOnSale);

        if (promotion != null) return CustomResponse(data: promotion.MapToDto());

        NotifyError("Promotion not found");

        return CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Creates a new promotion
    /// </summary>
    /// <param name="model">Promotion creation data</param>
    /// <returns>Newly created promotion</returns>
    /// <response code="201">Promotion successfully created</response>
    /// <response code="400">Invalid input data or business rule violation</response>
    [HttpPost]
    [ProducesResponseType(typeof(Root<PromotionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<PromotionResponse>>> AddPromotion(PromotionAddRequest model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<PromotionResponse>(ModelState);

        var entity = model.MapToEntity();

        foreach (var game in entity.GamesOnSale)
        {
            game.PromotionId = entity.Id;
        }

        var result = await promotionService.AddAsync(entity);

        return result
            ? CustomResponse(data: entity.MapToDto(), statusCode: HttpStatusCode.Created)
            : CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Updates an existing promotion
    /// </summary>
    /// <param name="id">Promotion ID to update</param>
    /// <param name="model">Updated promotion data</param>
    /// <response code="204">Promotion successfully updated</response>
    /// <response code="400">Invalid data or business rule violation</response>
    /// <response code="404">Promotion not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<PromotionResponse>>> UpdatePromotion(Guid id, PromotionUpdateRequest model)
    {
        if (id != model.Id)
        {
            NotifyError("The ids entered are not the same");
            return CustomResponse<PromotionResponse>();
        }

        if (!ModelState.IsValid) return CustomModelStateResponse<PromotionResponse>(ModelState);

        var result = await promotionService.UpdateAsync(id, model.MapToEntity());

        if (result != null)
            return !result.Value
                ? CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Deletes a promotion
    /// </summary>
    /// <param name="id">Promotion ID to delete</param>
    /// <response code="204">Promotion successfully deleted</response>
    /// <response code="400">Cannot delete (contains games or other restrictions)</response>
    /// <response code="404">Promotion not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<PromotionResponse>>> DeletePromotion(Guid id)
    {
        var result = await promotionService.DeleteAsync(id);

        if (result != null)
            return !result.Value
                ? CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Adds games to an existing promotion
    /// </summary>
    /// <param name="promotionId">Target promotion ID</param>
    /// <param name="model">List of games to add</param>
    /// <returns>Added promotion items</returns>
    /// <response code="201">Games successfully added to promotion</response>
    /// <response code="400">Invalid data or game already in promotion</response>
    /// <response code="404">Promotion not found</response>
    [HttpPost("{promotionId:guid}/promotion-games")]
    [ProducesResponseType(typeof(Root<IEnumerable<PromotionGameResponse>>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<IEnumerable<PromotionGameResponse>>>> AddPromotionGame(Guid promotionId,
        List<PromotionGameAddRequest> model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<IEnumerable<PromotionGameResponse>>(ModelState);

        var entities = model.Select(x => x.MapToEntity()).ToList();

        foreach (var entity in entities)
        {
            entity.PromotionId = promotionId;
        }

        var result = await promotionService.AddGamesOnSaleAsync(promotionId, entities);

        if (result != null)
            return !result.Value
                ? CustomResponse<IEnumerable<PromotionGameResponse>>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse(data: entities.Select(x => x.MapToDto()),
                    HttpStatusCode.Created);

        return CustomResponse<IEnumerable<PromotionGameResponse>>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Updates a promotion game item
    /// </summary>
    /// <param name="promotionGameId">Promotion item ID</param>
    /// <param name="model">Updated discount data</param>
    /// <response code="204">Promotion item successfully updated</response>
    /// <response code="400">Invalid discount value</response>
    /// <response code="404">Promotion item not found</response>
    [HttpPut("promotion-games/{promotionGameId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<PromotionGameResponse>>> UpdatePromotionGame(
        Guid promotionGameId,
        PromotionGameUpdateRequest model)
    {
        if (!ModelState.IsValid)
            return CustomModelStateResponse<PromotionGameResponse>(ModelState);

        var entity = model.MapToEntity();

        var result = await promotionService.UpdatePromotionGameAsync(promotionGameId, entity);

        if (result != null)
            return !result.Value
                ? CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Removes a game from promotion
    /// </summary>
    /// <param name="promotionGameId">Promotion item ID to remove</param>
    /// <response code="204">Game successfully removed from promotion</response>
    /// <response code="400">Cannot remove (has existing transactions)</response>
    /// <response code="404">Promotion item not found</response>
    [HttpDelete("promotion-games/{promotionGameId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<PromotionGameResponse>>> RemovePromotionGame(Guid promotionGameId)
    {
        var result = await promotionService.DeletePromotionGameAsync(promotionGameId);

        if (result != null)
            return !result.Value
                ? CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NotFound);
    }
}