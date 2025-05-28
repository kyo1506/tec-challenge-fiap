using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Linq.Expressions;
using TecChallenge.Domain.Entities;
using TecChallenge.Domain.Entities.Validations;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Domain.Notifications;
using TecChallenge.Domain.Services;

namespace TecChallenge.Tests;

public class GameServiceTest
{
    private readonly Mock<INotifier> _notifierMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly GameService _gameService;
    public GameServiceTest()
    {
        _notifierMock = new Mock<INotifier>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _gameService = new GameService(
            _notifierMock.Object,
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object
        );

        var transactionMock = new Mock<IDbContextTransaction>();

        _unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
    }

    [Fact]
    public async Task AddGame_ValidAndSuccess()
    {
        var game = new Game
        {
            Name = "God Of War",
            Price = 199
        };

        _gameRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Game, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _gameService.AddAsync(game);
        
        result.Should().BeTrue();

        _gameRepositoryMock.Verify(r => r.AddAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifierMock.Verify(n => n.Handle(It.IsAny<Notification>()), Times.Never);

    }

    [Fact]
    public async Task AddGame_ErrorGameExists()
    {
        var game = new Game
        {
            Name = "God Of War",
            Price = 199
        };

        _gameRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Game, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _gameService.AddAsync(game);
        result.Should().BeFalse();

        _gameRepositoryMock.Verify(r => r.AddAsync(game, It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(n => n.Message == "There is already a game with this name in the records")), Times.Once);
    }

    [Fact]
    public async Task AddGame_InvalidModel()
    {
        var game = new Game { Name = "", Price = 0 }; 

        var result = await _gameService.AddAsync(game);

        result.Should().BeFalse();

        _gameRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task AddGame_AddThrowsException()
    {
        var game = new Game { Name = "God Of War", Price = 199 };

        _gameRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Game, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _gameRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Add exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _gameService.AddAsync(game);

        await act.Should().ThrowAsync<Exception>().WithMessage("Add exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddGame_CommitThrowsException()
    {
        var game = new Game { Name = "God Of War", Price = 199 };

        _gameRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Game, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _gameRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _gameService.AddAsync(game);

        await act.Should().ThrowAsync<Exception>().WithMessage("Commit exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGame_ValidAndSuccess()
    {
        var id = Guid.NewGuid();
        var existingGame = new Game { Name = "God Of War", Price = 100 };
        var updatedGame = new Game { Name = "God Of War", Price = 150, IsActive = true };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGame);

        _gameRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Game, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _gameService.UpdateAsync(id, updatedGame);

        result.Should().BeTrue();

        _gameRepositoryMock.Verify(r => r.Update(It.Is<Game>(g => g.Name == updatedGame.Name && g.Price == updatedGame.Price && g.IsActive == updatedGame.IsActive)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGame_InvalidModel()
    {
        var id = Guid.NewGuid();
        var invalidGame = new Game { Name = "", Price = 0 };

        var result = await _gameService.UpdateAsync(id, invalidGame);

        result.Should().BeFalse();

        _gameRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _gameRepositoryMock.Verify(r => r.Update(It.IsAny<Game>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateGame_GameNotFound()
    {
        var id = Guid.NewGuid();
        var updatedGame = new Game { Name = "God Of War", Price = 150 };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game)null);

        var result = await _gameService.UpdateAsync(id, updatedGame);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(r => r.Update(It.IsAny<Game>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(not => not.Message.Contains("Game not found"))), Times.Once);
    }

    [Fact]
    public async Task UpdateGame_DuplicateName()
    {
        var id = Guid.NewGuid();
        var existingGame = new Game { Name = "God Of War", Price = 100 };
        var updatedGame = new Game { Name = "God Of War 2", Price = 150 };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGame);

        _gameRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Game, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _gameService.UpdateAsync(id, updatedGame);

        result.Should().BeFalse();

        _gameRepositoryMock.Verify(r => r.Update(It.IsAny<Game>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(not => not.Message.Contains("There is already a game with this name in the records"))), Times.Once);
    }

    [Fact]
    public async Task UpdateGame_ThrowsException()
    {
        var id = Guid.NewGuid();
        var existingGame = new Game { Name = "God Of War", Price = 100, IsActive = false };
        var updatedGame = new Game { Name = "God Of War", Price = 150, IsActive = true };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGame);

        _gameRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Game, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _gameRepositoryMock
            .Setup(r => r.Update(It.IsAny<Game>()))
            .Throws(new Exception("Update exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _gameService.UpdateAsync(id, updatedGame);

        await act.Should().ThrowAsync<Exception>().WithMessage("Update exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task DeleteGame_ValidAndSuccess()
    {
        var id = Guid.NewGuid();
        var existingGame = new Game { Name = "God Of War", Price = 100 };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGame);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _gameService.DeleteAsync(id);

        result.Should().BeTrue();

        _gameRepositoryMock.Verify(r => r.Update(It.Is<Game>(g => g.IsActive == false)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGame_GameNotFound()
    {
        var id = Guid.NewGuid();

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game)null);

        var result = await _gameService.DeleteAsync(id);

        result.Should().BeNull();

        _gameRepositoryMock.Verify(r => r.Update(It.IsAny<Game>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(not => not.Message.Contains("Game not found"))), Times.Once);
    }

    [Fact]
    public async Task DeleteGame_ThrowsException()
    {
        var id = Guid.NewGuid();
        var existingGame = new Game { Name = "God Of War", Price = 100 };

        _gameRepositoryMock
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGame);

        _gameRepositoryMock
            .Setup(r => r.Update(It.IsAny<Game>()))
            .Throws(new Exception("Delete exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _gameService.DeleteAsync(id);

        await act.Should().ThrowAsync<Exception>().WithMessage("Delete exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
