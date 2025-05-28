using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TecChallenge.Domain.Entities;
using TecChallenge.Domain.Entities.Enums;
using TecChallenge.Domain.Exceptions;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Domain.Services;

namespace TecChallenge.Tests;

public class TransactionServiceTest
{
    private readonly Mock<IUserWalletRepository> _walletRepositoryMock;
    private readonly Mock<IUserLibraryRepository> _libraryRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IPromotionRepository> _promotionRepositoryMock;
    private readonly Mock<IWalletTransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly TransactionService _transactionService;
    public TransactionServiceTest()
    {
        _walletRepositoryMock = new Mock<IUserWalletRepository>();
        _libraryRepositoryMock = new Mock<IUserLibraryRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _promotionRepositoryMock = new Mock<IPromotionRepository>();
        _transactionRepositoryMock = new Mock<IWalletTransactionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionService = new TransactionService(
            _walletRepositoryMock.Object,
            _libraryRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _promotionRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _unitOfWorkMock.Object
        );

        var transactionMock = new Mock<IDbContextTransaction>();

        _unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
    }

    [Fact]
    public async Task ProcessPurchase_Success()
    {
        var userId = Guid.NewGuid();
        var promotionGameId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m); 

        var library = new UserLibrary { UserId = userId, Items = new List<LibraryItem>() };
        var game = new Game
        {
            Name = "Test Game",
            Description = "Test Game Description",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        }; 
        
        var promotionGame = new PromotionGame { PromotionId = promotionGameId, GameId = game.Id, DiscountPercentage = 10m };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Game, bool>>>(),
            true,
            It.IsAny<Expression<Func<Game, object>>[]>()
        )).ReturnsAsync(game);

        _promotionRepositoryMock.Setup(x => x.GetPromotionGameById(promotionGameId))
            .ReturnsAsync(promotionGame);

        var result = await _transactionService.ProcessPurchaseAsync(userId, game.Id, promotionGameId);

        Assert.NotNull(result);
        Assert.Equal(game.Name, result.GameName);
        Assert.Equal(promotionGame.DiscountPercentage, result.DiscountPercentage);
        Assert.Equal(game.Price, result.Price);
        Assert.Equal(wallet.Balance, result.Balance);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task ProcessPurchase_InsufficientBalanceException()
    {
        var userId = Guid.NewGuid();
        var wallet = new UserWallet(userId);
        wallet.Deposit(10m); 

        var library = new UserLibrary { UserId = userId, Items = new List<LibraryItem>() };
        var game = new Game
        {
            Name = "Game",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Game, bool>>>(),
            true,
            It.IsAny<Expression<Func<Game, object>>[]>()
        )).ReturnsAsync(game);

        _promotionRepositoryMock.Setup(x => x.GetPromotionGameById(It.IsAny<Guid>()))
            .ReturnsAsync((PromotionGame?)null);

        var exception = await Assert.ThrowsAsync<InsufficientBalanceException>(async () =>
            await _transactionService.ProcessPurchaseAsync(userId, game.Id, null));

        Assert.IsType<InsufficientBalanceException>(exception);
    }

    [Fact]
    public async Task ProcessPurchase_ValidPromotionSuccess()
    {
        var userId = Guid.NewGuid();
        var promotionGameId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        var library = new UserLibrary { UserId = userId, Items = new List<LibraryItem>() };

        var game = new Game
        {
            Name = "Test Game",
            Description = "Test Game Description",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };

        var promotionGame = new PromotionGame
        {
            PromotionId = promotionGameId,
            GameId = game.Id,
            DiscountPercentage = 20m 
        };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()))
            .ReturnsAsync(game);

        _promotionRepositoryMock.Setup(x => x.GetPromotionGameById(promotionGameId))
            .ReturnsAsync(promotionGame);

        var result = await _transactionService.ProcessPurchaseAsync(userId, game.Id, promotionGameId);

        Assert.NotNull(result);
        Assert.Equal(game.Name, result.GameName);
        Assert.Equal(promotionGame.DiscountPercentage, result.DiscountPercentage);
        Assert.Equal(game.Price, result.Price);
        Assert.Equal(wallet.Balance, result.Balance); 

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPurchase_WalletNotFoundException()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync((UserWallet?)null);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(new UserLibrary { UserId = userId, Items = new List<LibraryItem>() });

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>())).ReturnsAsync(new Game
            {
                Name = "Test Game",
                Price = 50m,
                IsActive = true,
                ReleaseDate = DateTime.UtcNow
            });

        var exception = await Assert.ThrowsAsync<DomainException>(async () =>
            await _transactionService.ProcessPurchaseAsync(userId, gameId, null));

        Assert.Equal("Wallet not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPurchase_LibraryNotFoundException()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(new UserWallet(userId));

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync((UserLibrary?)null);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                null))
            .ReturnsAsync(new Game
            {
                Name = "Test Game",
                Price = 50m,
                IsActive = true,
                ReleaseDate = DateTime.UtcNow
            });

        var exception = await Assert.ThrowsAsync<DomainException>(async () =>
            await _transactionService.ProcessPurchaseAsync(userId, gameId, null));

        Assert.Equal("Library not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPurchase_GameNotFoundException()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(new UserWallet(userId));

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(new UserLibrary { UserId = userId, Items = new List<LibraryItem>() });

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                null))
            .ReturnsAsync((Game?)null);

        var exception = await Assert.ThrowsAsync<DomainException>(async () =>
            await _transactionService.ProcessPurchaseAsync(userId, gameId, null));

        Assert.Equal("Game not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPurchaseAsync_Should_Throw_DomainException_When_Game_Already_In_Library()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        var library = new UserLibrary
        {
            UserId = userId,
            Items = new List<LibraryItem>
        {
            new LibraryItem { GameId = gameId } 
        }
        };

        var game = new Game
        {
            Name = "Test Game",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()))
            .ReturnsAsync(game);

        var exception = await Assert.ThrowsAsync<DomainException>(async () =>
            await _transactionService.ProcessPurchaseAsync(userId, gameId, null));

        Assert.Equal("Game already in library", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPurchase_PromotionNotApplicableException()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var promotionGameId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        var library = new UserLibrary
        {
            UserId = userId,
            Items = new List<LibraryItem>()
        };

        var game = new Game
        {
            Name = "Test Game",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()))
            .ReturnsAsync(game);

        _promotionRepositoryMock.Setup(x => x.GetPromotionGameById(promotionGameId))
            .ReturnsAsync((PromotionGame?)null);

        var exception = await Assert.ThrowsAsync<PromotionNotApplicableException>(async () =>
            await _transactionService.ProcessPurchaseAsync(userId, gameId, promotionGameId));

        Assert.Equal("Promotion not applicable to this game", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPurchase_CommitException()
    {
        var userId = Guid.NewGuid();
        var promotionGameId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        var library = new UserLibrary { UserId = userId, Items = new List<LibraryItem>() };

        var game = new Game
        {
            Name = "Test Game",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };

        var promotionGame = new PromotionGame
        {
            PromotionId = promotionGameId,
            GameId = game.Id,
            DiscountPercentage = 10m
        };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()))
            .ReturnsAsync(game);

        _promotionRepositoryMock.Setup(x => x.GetPromotionGameById(promotionGameId))
            .ReturnsAsync(promotionGame);

        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit failed"));

        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await _transactionService.ProcessPurchaseAsync(userId, game.Id, promotionGameId));

        Assert.Equal("Commit failed", exception.Message);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefundedPurchase_Success()
    {
        var userId = Guid.NewGuid();
        var wallet = new UserWallet(userId);
        var gamePrice = 50m;

        wallet.Deposit(100m);

        var game = new Game
        {
            Name = "Test Game",
            Price = gamePrice,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };
        var library = new UserLibrary { UserId = userId, Items = new List<LibraryItem>() };

        wallet.PurchaseGame(game, null, library);

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()))
            .ReturnsAsync(game);

        var result = await _transactionService.RefundedPurchaseAsync(userId, game.Id);

        Assert.NotNull(result);
        Assert.Equal(game.Name, result.GameName);
        Assert.Equal(gamePrice, result.RefundAmount);
        Assert.Equal(wallet.Balance, result.NewBalance);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task RefundedPurchase_WalletNotFound()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync((UserWallet)null!);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _transactionService.RefundedPurchaseAsync(userId, gameId));

        Assert.Equal("Wallet not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefundedPurchase_LibraryNotFound()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()))
            .ReturnsAsync((UserLibrary)null!);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _transactionService.RefundedPurchaseAsync(userId, gameId));

        Assert.Equal("Library not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefundedPurchase_GameNotFound()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        var library = new UserLibrary { UserId = userId, Items = new List<LibraryItem>() };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()))
            .ReturnsAsync((Game)null!);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _transactionService.RefundedPurchaseAsync(userId, gameId));

        Assert.Equal("Game not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefundedPurchase_TransactionNotFound()
    {
        var userId = Guid.NewGuid();
        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        var library = new UserLibrary { UserId = userId, Items = new List<LibraryItem>() };

        var game = new Game
        {
            Name = "Test Game",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()))
            .ReturnsAsync(game);


        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _transactionService.RefundedPurchaseAsync(userId, game.Id));

        Assert.Equal("Purchase transaction not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefundedPurchase_CommitException()
    {
        var userId = Guid.NewGuid();

        var wallet = new UserWallet(userId);
        wallet.Deposit(100m);

        var game = new Game
        {
            Name = "Test Game",
            Price = 50m,
            IsActive = true,
            ReleaseDate = DateTime.UtcNow
        };

        var library = new UserLibrary
        {
            UserId = userId,
            Items = new List<LibraryItem>()
        };

        wallet.PurchaseGame(game, null, library);

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()
            ))
            .ReturnsAsync(wallet);

        _libraryRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserLibrary, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserLibrary, object>>[]>()
            ))
            .ReturnsAsync(library);

        _gameRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Game, bool>>>(),
                true,
                It.IsAny<Expression<Func<Game, object>>[]>()
            ))
            .ReturnsAsync(game);

        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit failed"));

        await Assert.ThrowsAsync<Exception>(() =>
            _transactionService.RefundedPurchaseAsync(userId, game.Id));

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_Should_ReturnDepositResponse_When_Success()
    {
        var userId = Guid.NewGuid();
        var amount = 100m;
        var wallet = new UserWallet(userId);

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        var result = await _transactionService.DepositAsync(userId, amount);

        Assert.NotNull(result);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(wallet.Balance, result.NewBalance);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task Deposit_WalletNotFound()
    {
        var userId = Guid.NewGuid();
        var amount = 100m;

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync((UserWallet?)null);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _transactionService.DepositAsync(userId, amount));

        Assert.Equal("Wallet not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Deposit_CommitException()
    {
        var userId = Guid.NewGuid();
        var amount = 100m;
        var wallet = new UserWallet(userId);

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit failed"));

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _transactionService.DepositAsync(userId, amount));

        Assert.Equal("Commit failed", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Withdrawal_Success()
    {
        var userId = Guid.NewGuid();
        var amount = 50m;
        var wallet = new UserWallet(userId);
        wallet.Deposit(100m); 

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        var result = await _transactionService.WithdrawalAsync(userId, amount);

        Assert.NotNull(result);
        Assert.Equal(amount, result.Amount);
        Assert.Equal(wallet.Balance, result.NewBalance);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Withdrawal_WalletNotFound()
    {
        var userId = Guid.NewGuid();
        var amount = 50m;

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync((UserWallet?)null);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _transactionService.WithdrawalAsync(userId, amount));

        Assert.Equal("Wallet not found", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Withdrawal_CommitException()
    {
        var userId = Guid.NewGuid();
        var amount = 50m;
        var wallet = new UserWallet(userId);
        wallet.Deposit(100m); 

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit failed"));

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _transactionService.WithdrawalAsync(userId, amount));

        Assert.Equal("Commit failed", exception.Message);

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Withdrawal_InsufficientBalance()
    {
        var userId = Guid.NewGuid();
        var amount = 100m; 
        var wallet = new UserWallet(userId);
        wallet.Deposit(50m); 

        _walletRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<UserWallet, bool>>>(),
                true,
                It.IsAny<Expression<Func<UserWallet, object>>[]>()))
            .ReturnsAsync(wallet);

        var exception = await Assert.ThrowsAsync<InsufficientBalanceException>(() =>
            _transactionService.WithdrawalAsync(userId, amount));

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
