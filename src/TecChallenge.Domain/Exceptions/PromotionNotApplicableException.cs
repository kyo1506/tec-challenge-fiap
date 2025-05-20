namespace TecChallenge.Domain.Exceptions;

public class PromotionNotApplicableException : DomainException
{
    public Guid? GameId { get; }
    public Guid? PromotionGameId { get; }

    public PromotionNotApplicableException(string message) 
        : base(message)
    {
    }

    public PromotionNotApplicableException(Guid gameId, Guid promotionGameId) 
        : base($"A promoção {promotionGameId} não é aplicável ao jogo {gameId}")
    {
        GameId = gameId;
        PromotionGameId = promotionGameId;
    }

    public PromotionNotApplicableException(string message, Guid gameId, Guid promotionGameId) 
        : base(message)
    {
        GameId = gameId;
        PromotionGameId = promotionGameId;
    }
}