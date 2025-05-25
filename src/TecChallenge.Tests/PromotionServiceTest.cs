using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TecChallenge.Domain.Entities;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Domain.Notifications;
using TecChallenge.Domain.Services;

namespace TecChallenge.Tests;

public class PromotionServiceTest
{
    private readonly Mock<INotifier> _notifierMock;
    private readonly Mock<IPromotionRepository> _promotionRepositoryMock;
    private readonly Mock<IPromotionGameRepository> _promotionGameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PromotionService _promotionService;
    public PromotionServiceTest()
    {
        _notifierMock = new Mock<INotifier>();
        _promotionRepositoryMock = new Mock<IPromotionRepository>();
        _promotionGameRepositoryMock = new Mock<IPromotionGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _promotionService = new PromotionService(
            _notifierMock.Object,
            _promotionRepositoryMock.Object,
            _promotionGameRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }
    [Fact]
    public async Task AddPromotion_ValidAndSuccess()
    {
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.AddAsync(promotion);
        result.Should().BeTrue();

        _promotionRepositoryMock.Verify(r => r.AddAsync(promotion, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifierMock.Verify(n => n.Handle(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task AddPromotion_ErrorPromotionExists()
    {
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.AddAsync(promotion);
        result.Should().BeFalse();

        _promotionRepositoryMock.Verify(r => r.AddAsync(promotion, It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(n => n.Message == "There is already a promotion with this name in the records")), Times.Once);
    }

    [Fact]
    public async Task AddPromotion_InvalidModel()
    {
        var promotion = new Promotion
        {
            Name = "",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(-15)
        };
        var result = await _promotionService.AddAsync(promotion);
        result.Should().BeFalse();

        _promotionRepositoryMock.Verify(r => r.AddAsync(promotion, It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddPromotion_AddThrowsException()
    {
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _promotionRepositoryMock
            .Setup(p => p.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Add exception"));


        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async() => await _promotionService.AddAsync(promotion);

        await act.Should().ThrowAsync<Exception>().WithMessage("Add exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    public async Task AddPromotion_CommitThrowsException()
    {
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _promotionRepositoryMock
            .Setup(p => p.AddAsync(It.IsAny<Promotion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _promotionService.AddAsync(promotion);

        await act.Should().ThrowAsync<Exception>().WithMessage("Commit exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePromotion_ValidAndSuccess()
    {
        var id = Guid.NewGuid();
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        var promotionUpdate = new Promotion
        {
            Name = "Promotion Test Update",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(x => x.Id == id, true))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.UpdateAsync(id, promotionUpdate);
        result.Should().BeTrue();

        _promotionRepositoryMock.Verify(r => 
            r.Update(It.Is<Promotion>(p => p.Name == promotionUpdate.Name && p.StartDate == promotionUpdate.StartDate && p.EndDate == promotionUpdate.EndDate)),
                Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task UpdatePromotion_InvalidModel()
    {
        var id = Guid.NewGuid();
        var promotionUpdate = new Promotion
        {
            Name = "Promotion Test Update",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(-5)
        };

        var result = await _promotionService.UpdateAsync(id,promotionUpdate);
        result.Should().BeFalse();

        _promotionRepositoryMock.Verify(p => p.FirstOrDefaultAsync(x => x.Id == id, true), Times.Never);
        _promotionRepositoryMock.Verify(p => p.Update(It.IsAny<Promotion>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePromotion_PromotionNotFound()
    {
        var id = Guid.NewGuid();
        var promotionUpdate = new Promotion
        {
            Name = "Promotion Test Update",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(5)
        };
        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(x => x.Id == id, true))
            .ReturnsAsync((Promotion)null);

        var result = await _promotionService.UpdateAsync(id, promotionUpdate);
        result.Should().BeNull();

        _promotionRepositoryMock.Verify(p => p.Update(It.IsAny<Promotion>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(not => not.Message.Contains("Promotion not found"))), Times.Once);

    }

    [Fact]
    public async Task UpdatePromotion_DuplicateName()
    {
        var id = Guid.NewGuid();
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        var promotionUpdate = new Promotion
        {
            Name = "Promotion Test Update",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        _promotionRepositoryMock
                    .Setup(p => p.FirstOrDefaultAsync(x => x.Id == id, true))
                    .ReturnsAsync(promotion);

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.UpdateAsync(id, promotionUpdate);
        result.Should().BeFalse();

        _promotionRepositoryMock.Verify(p => p.Update(It.IsAny<Promotion>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(not => not.Message.Contains("There is already a promotion with this name in the records"))), Times.Once);
    }
    [Fact]
    public async Task UpdatePromotion_UpdateThrowsException()
    {
        var id = Guid.NewGuid();
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        var promotionUpdate = new Promotion
        {
            Name = "Promotion Test Update",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(x => x.Id == id, true))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _promotionRepositoryMock
            .Setup(p => p.Update(It.IsAny<Promotion>()))
            .Throws(new Exception("Update exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _promotionService.UpdateAsync(id, promotionUpdate);
        await act.Should().ThrowAsync<Exception>().WithMessage("Update exception");

        _promotionRepositoryMock.Verify(p => p.Update(It.IsAny<Promotion>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task UpdatePromotion_CommitThrowsException()
    {
        var id = Guid.NewGuid();
        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(15)
        };

        var promotionUpdate = new Promotion
        {
            Name = "Promotion Test Update",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(5)
        };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(x => x.Id == id, true))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock
            .Setup(p => p.AnyAsync(It.IsAny<Expression<Func<Promotion, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _promotionService.UpdateAsync(id, promotionUpdate);
        await act.Should().ThrowAsync<Exception>().WithMessage("Commit exception");

        _promotionRepositoryMock.Verify(p => p.Update(It.IsAny<Promotion>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePromotion_Success()
    {
        var id = Guid.NewGuid();

        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            GamesOnSale = []
        };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync(promotion);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.DeleteAsync(id);

        result.Should().BeTrue();

        _promotionRepositoryMock.Verify(p => p.Delete(It.IsAny<Promotion>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePromotion_NotFound()
    {
        var id = Guid.NewGuid();

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync((Promotion)null);

        var result = await _promotionService.DeleteAsync(id);

        result.Should().BeNull();

        _promotionRepositoryMock.Verify(p => p.Delete(It.IsAny<Promotion>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task DeletePromotion_WithGamesOnSale_CannotDelete()
    {
        var id = Guid.NewGuid();

        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            GamesOnSale = [new PromotionGame()]
        };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync(promotion);

        var result = await _promotionService.DeleteAsync(id);

        result.Should().BeFalse();

        _promotionRepositoryMock.Verify(p => p.Delete(It.IsAny<Promotion>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePromotion_CommitThrowsException()
    {
        var id = Guid.NewGuid();

        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            GamesOnSale = []
        };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync(promotion);

        _promotionRepositoryMock
            .Setup(p => p.Delete(It.IsAny<Promotion>()));

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _promotionService.DeleteAsync(id);

        await act.Should().ThrowAsync<Exception>().WithMessage("Commit exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddGamesOnSale_PromotionNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();

        var model = new PromotionGame();
        model.PromotionId = Guid.NewGuid();
        model.GameId = Guid.NewGuid();
        model.DiscountPercentage = 20;

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(It.IsAny<Expression<Func<PromotionGame, bool>>>(), true))
            .ReturnsAsync((PromotionGame)null);

        var result = await _promotionService.UpdatePromotionGameAsync(id, model);

        result.Should().BeNull();

        _promotionGameRepositoryMock.Verify(p => p.Update(It.IsAny<PromotionGame>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddGamesOnSale_AllGamesAlreadyExist_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var existingGame = new PromotionGame { GameId = gameId };

        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            GamesOnSale = [existingGame]
        };

        var gamesOnSale = new List<PromotionGame> { existingGame };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync(promotion);

        var result = await _promotionService.AddGamesOnSaleAsync(id, gamesOnSale);

        result.Should().BeFalse();

        _promotionRepositoryMock.Verify(p => p.Update(It.IsAny<Promotion>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddGamesOnSale_GameInOtherPromotion_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            GamesOnSale = []
        };

        var newGame = new PromotionGame { GameId = gameId };
        var gamesOnSale = new List<PromotionGame> { newGame };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync(promotion);

        _promotionGameRepositoryMock
            .Setup(pg => pg.AnyAsync(
                It.IsAny<Expression<Func<PromotionGame, bool>>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(true);

        var result = await _promotionService.AddGamesOnSaleAsync(id, gamesOnSale);

        result.Should().BeFalse();

        _promotionRepositoryMock.Verify(p => p.Update(It.IsAny<Promotion>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddGamesOnSale_Success_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            GamesOnSale = []
        };

        var newGame = new PromotionGame { GameId = gameId };
        var gamesOnSale = new List<PromotionGame> { newGame };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync(promotion);

        _promotionGameRepositoryMock
            .Setup(pg => pg.AnyAsync(
                It.IsAny<Expression<Func<PromotionGame, bool>>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.AddGamesOnSaleAsync(id, gamesOnSale);

        result.Should().BeTrue();

        _promotionRepositoryMock.Verify(p => p.Update(It.Is<Promotion>(x => x.GamesOnSale.Contains(newGame))), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddGamesOnSale_CommitThrowsException()
    {
        var id = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var promotion = new Promotion
        {
            Name = "Promotion Test",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            GamesOnSale = []
        };

        var newGame = new PromotionGame { GameId = gameId };
        var gamesOnSale = new List<PromotionGame> { newGame };

        _promotionRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Promotion, bool>>>(),
                true,
                It.IsAny<Expression<Func<Promotion, object>>[]>()
            ))
            .ReturnsAsync(promotion);

        _promotionGameRepositoryMock
            .Setup(pg => pg.AnyAsync(
                It.IsAny<Expression<Func<PromotionGame, bool>>>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(false);

        _promotionRepositoryMock
            .Setup(p => p.Update(It.IsAny<Promotion>()));

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _promotionService.AddGamesOnSaleAsync(id, gamesOnSale);

        await act.Should().ThrowAsync<Exception>().WithMessage("Commit exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task UpdatePromotionGame_PromotionGameNotFound()
    {
        var id = Guid.NewGuid();

        var model = new PromotionGame();
        model.PromotionId = Guid.NewGuid();
        model.GameId = Guid.NewGuid();
        model.DiscountPercentage = 20;

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(It.IsAny<Expression<Func<PromotionGame, bool>>>(), true))
            .ReturnsAsync((PromotionGame)null);

        var result = await _promotionService.UpdatePromotionGameAsync(id, model);

        result.Should().BeNull();

        _promotionGameRepositoryMock.Verify(p => p.Update(It.IsAny<PromotionGame>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePromotionGame_PromotionOrGameChanged_()
    {
        var id = Guid.NewGuid();

        var existing = new PromotionGame();
        existing.PromotionId = Guid.NewGuid();
        existing.GameId = Guid.NewGuid();
        existing.DiscountPercentage = 10;

        var model = new PromotionGame();
        model.PromotionId = Guid.NewGuid();
        model.GameId = existing.GameId;
        model.DiscountPercentage = 20;

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(It.IsAny<Expression<Func<PromotionGame, bool>>>(), true))
            .ReturnsAsync(existing);

        var result = await _promotionService.UpdatePromotionGameAsync(id, model);

        result.Should().BeNull();

        _promotionGameRepositoryMock.Verify(p => p.Update(It.IsAny<PromotionGame>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePromotionGame_Success()
    {
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var existing = new PromotionGame();
        existing.PromotionId = promotionId;
        existing.GameId = gameId;
        existing.DiscountPercentage = 10;

        var model = new PromotionGame();
        model.PromotionId = promotionId;
        model.GameId = gameId;
        model.DiscountPercentage = 25;

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(It.IsAny<Expression<Func<PromotionGame, bool>>>(), true))
            .ReturnsAsync(existing);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.UpdatePromotionGameAsync(id, model);

        result.Should().BeTrue();
        existing.DiscountPercentage.Should().Be(model.DiscountPercentage);

        _promotionGameRepositoryMock.Verify(p => p.Update(It.Is<PromotionGame>(pg => pg.DiscountPercentage == model.DiscountPercentage)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePromotionGame_CommitThrowsException()
    {
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var existing = new PromotionGame();
        existing.PromotionId = promotionId;
        existing.GameId = gameId;
        existing.DiscountPercentage = 10;

        var model = new PromotionGame();
        model.PromotionId = promotionId;
        model.GameId = gameId;
        model.DiscountPercentage = 30;

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(It.IsAny<Expression<Func<PromotionGame, bool>>>(), true))
            .ReturnsAsync(existing);

        _promotionGameRepositoryMock
            .Setup(p => p.Update(It.IsAny<PromotionGame>()));

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit failed"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _promotionService.UpdatePromotionGameAsync(id, model);

        await act.Should().ThrowAsync<Exception>().WithMessage("Commit failed");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePromotionGame_PromotionGameNotFound()
    {
        var id = Guid.NewGuid();

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<PromotionGame, bool>>>(),
                true,
                It.IsAny<Expression<Func<PromotionGame, object>>[]>()
            ))
            .ReturnsAsync((PromotionGame)null);

        var result = await _promotionService.DeletePromotionGameAsync(id);

        result.Should().BeNull();

        _promotionGameRepositoryMock.Verify(p => p.Delete(It.IsAny<PromotionGame>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePromotionGame_WithTransactions()
    {
        var promotionGame = new PromotionGame();
        promotionGame.WalletTransactions.Add(new WalletTransaction());

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<PromotionGame, bool>>>(),
                true,
                It.IsAny<Expression<Func<PromotionGame, object>>[]>()))
            .ReturnsAsync(promotionGame);

        var result = await _promotionService.DeletePromotionGameAsync(promotionGame.Id);

        result.Should().BeFalse();

        _promotionGameRepositoryMock.Verify(p => p.Delete(It.IsAny<PromotionGame>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePromotionGame_Success()
    {
        var promotionGame = new PromotionGame(); 

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<PromotionGame, bool>>>(),
                true,
                It.IsAny<Expression<Func<PromotionGame, object>>[]>()))
            .ReturnsAsync(promotionGame);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _promotionService.DeletePromotionGameAsync(promotionGame.Id);

        result.Should().BeTrue();

        _promotionGameRepositoryMock.Verify(p => p.Delete(It.Is<PromotionGame>(pg => pg == promotionGame)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePromotionGame_CommitThrowsException()
    {
        var promotionGame = new PromotionGame();

        _promotionGameRepositoryMock
            .Setup(p => p.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<PromotionGame, bool>>>(),
                true,
                It.IsAny<Expression<Func<PromotionGame, object>>[]>()))
            .ReturnsAsync(promotionGame);

        _promotionGameRepositoryMock
            .Setup(p => p.Delete(It.IsAny<PromotionGame>()));

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit failed"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _promotionService.DeletePromotionGameAsync(promotionGame.Id);

        await act.Should().ThrowAsync<Exception>().WithMessage("Commit failed");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
