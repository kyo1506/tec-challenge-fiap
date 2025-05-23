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


    
}
