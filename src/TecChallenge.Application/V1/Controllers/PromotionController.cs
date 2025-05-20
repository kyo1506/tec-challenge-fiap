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
    public async Task<ActionResult<Root<PromotionResponse>>> AddPromotion(PromotionRequest model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<PromotionResponse>(ModelState);

        var entity = model.MapToEntity();

        var result = await promotionService.AddAsync(entity);

        return result
            ? CustomResponse(data: entity.MapToDto(), statusCode: HttpStatusCode.Created)
            : CustomResponse<PromotionResponse>(statusCode: HttpStatusCode.BadRequest);
    }
    
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Root<PromotionResponse>>> UpdatePromotion(Guid id, PromotionRequest model)
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
}