using TecChallenge.Domain.Entities;
using TecChallenge.Shared.Models.Dtos;
using TecChallenge.Shared.Models.Dtos.Requests;
using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Application.Extensions;

public static class MappingEntityExtension
{
    public static Game MapToEntity(this GameAddRequest game)
    {
        return new Game
        {
            Name = game.Name,
            Price = game.Price
        };
    }

    public static Game MapToEntity(this GameUpdateRequest game)
    {
        return new Game
        {
            Name = game.Name,
            Price = game.Price,
            IsActive = game.IsActive
        };
    }
    
    public static Promotion MapToEntity(this PromotionAddRequest promotion)
    {
        return new Promotion
        {
            Name = promotion.Name,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            GamesOnSale = promotion.GamesOnSale
                .Select(x => x.MapToEntity())
                .ToList()
        };
    }

    public static Promotion MapToEntity(this PromotionUpdateRequest promotion)
    {
        return new Promotion
        {
            Name = promotion.Name,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate
        };
    }

    private static PromotionGame MapToEntity(this PromotionGameAddRequest promotionGame)
    {
        return new PromotionGame
        {
            PromotionId = promotionGame.PromotionId,
            GameId = promotionGame.GameId,
            DiscountPercentage = promotionGame.DiscountPercentage
        };
    }
}