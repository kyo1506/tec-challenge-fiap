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
    [HttpGet]
    public async Task<ActionResult<Root<IEnumerable<PromotionResponse>>>> GetAllPromotions()
    {
        var promotions = (await promotionRepository.GetAllAsync())
            .Select(g => g.MapToDto());

        return CustomResponse(data: promotions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Root<PromotionResponse>>> GetPromotionById(Guid id)
    {
        var promotion = await promotionRepository.FirstOrDefaultAsync(x => x.Id == id, includes: x => x.GamesOnSale);

        if (promotion != null) return CustomResponse(data: promotion.MapToDto());

        NotifyError("Promotion not found");

        return CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NotFound);
    }

    [HttpPost]
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
    
    [HttpPut("{id:guid}")]
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
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Root<PromotionResponse>>> DeletePromotion(Guid id)
    {
        var result = await promotionService.DeleteAsync(id);

        if (result != null)
            return !result.Value
                ? CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.NotFound);
    }
    
    [HttpPost("{id:guid}/games")]
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
    
    [HttpPut("{promotionGameId:guid}/games/{gameId:guid}")]
    public async Task<ActionResult<Root<PromotionGameResponse>>> UpdatePromotionGame(
        Guid promotionGameId,
        PromotionGameUpdateRequest model)
    {
        if (!ModelState.IsValid) 
            return CustomModelStateResponse<PromotionGameResponse>(ModelState);

        var updatedEntity = model.MapToEntity(promotionGame.Id);
        var result = await promotionService.UpdatePromotionGameAsync(promotionGame.Id, updatedEntity);

        if (result != null)
            return !result.Value
                ? CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NotFound);
    }

    [HttpDelete("{promotionId:guid}/games/{gameId:guid}")]
    public async Task<ActionResult<Root<PromotionGameResponse>>> RemoveGameFromPromotion(Guid promotionId, Guid gameId)
    {
        var result = await promotionService.DeletePromotionGameAsync(promotionGame.Id);

        if (result != null)
            return !result.Value
                ? CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.BadRequest)
                : CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NoContent);

        return CustomResponse<PromotionGameResponse>(statusCode: HttpStatusCode.NotFound);
    }
}