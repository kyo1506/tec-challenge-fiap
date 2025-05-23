using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Linq.Expressions;
using TecChallenge.Domain.Entities;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Domain.Notifications;
using TecChallenge.Domain.Services;

namespace TecChallenge.Tests;

public class UserLibraryServiceTest
{
    private readonly Mock<INotifier> _notifierMock;
    private readonly Mock<IUserLibraryRepository> _userLibraryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UserLibraryService _userLibraryService;
    public UserLibraryServiceTest()
    {
        _notifierMock = new Mock<INotifier>();
        _userLibraryRepositoryMock = new Mock<IUserLibraryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _userLibraryService = new UserLibraryService(
            _notifierMock.Object,
            _userLibraryRepositoryMock.Object,
            _unitOfWorkMock.Object
        );

        var transactionMock = new Mock<IDbContextTransaction>();

            _unitOfWorkMock
                .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
    }
    [Fact]
    public async Task AddLibrary_ValidAndSuccess()
    {
        var userLibrary = new UserLibrary
        {
            UserId = Guid.NewGuid(),
        };

        _userLibraryRepositoryMock
            .Setup(r => r.WhereAsync(It.IsAny<Expression<Func<UserLibrary, bool>>>()))
        .ReturnsAsync(new List<UserLibrary>());

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _userLibraryService.AddAsync(userLibrary);
        result.Should().BeTrue();

        _userLibraryRepositoryMock.Verify(r => r.AddAsync(userLibrary, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifierMock.Verify(n => n.Handle(It.IsAny<Notification>()), Times.Never);
    }
    [Fact]
    public async Task AddLibrary_ErrorLibraryExists()
    {
        var userLibrary = new UserLibrary
        {
            UserId = Guid.NewGuid(),
        };

        _userLibraryRepositoryMock
            .Setup(r => r.WhereAsync(It.IsAny<Expression<Func<UserLibrary, bool>>>()))
        .ReturnsAsync(new List<UserLibrary>{ userLibrary });

        var result = await _userLibraryService.AddAsync(userLibrary);
        result.Should().BeFalse();

        _userLibraryRepositoryMock.Verify(r => r.AddAsync(userLibrary, It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.Is<Notification>(n => n != null && n.Message == "There is already a library created for this user")), Times.Once);
    }

    [Fact]
    public async Task AddLibrary_InvalidModel()
    {
        var userLibrary = new UserLibrary
        {
            UserId = Guid.Empty,
        };

        var result = await _userLibraryService.AddAsync(userLibrary);
        result.Should().BeFalse();

        _userLibraryRepositoryMock.Verify(r => r.AddAsync(userLibrary, It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notifierMock.Verify(n => n.Handle(It.IsAny<Notification>()), Times.AtLeastOnce);
    }
    [Fact]
    public async Task AddLibrary_AddThrowsException()
    {
        var userLibrary = new UserLibrary
        {
            UserId = Guid.NewGuid(),
        };

        _userLibraryRepositoryMock
            .Setup(r => r.WhereAsync(It.IsAny<Expression<Func<UserLibrary, bool>>>()))
        .ReturnsAsync(new List<UserLibrary>());

        _userLibraryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserLibrary>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("Add exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _userLibraryService.AddAsync(userLibrary);
        await act.Should().ThrowAsync<Exception>().WithMessage("Add exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task AddLibrary_CommithrowsException()
    {
        var userLibrary = new UserLibrary
        {
            UserId = Guid.NewGuid(),
        };

        _userLibraryRepositoryMock
            .Setup(r => r.WhereAsync(It.IsAny<Expression<Func<UserLibrary, bool>>>()))
        .ReturnsAsync(new List<UserLibrary>());

        _userLibraryRepositoryMock.Setup(r => r.AddAsync(It.IsAny<UserLibrary>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Commit exception"));

        _unitOfWorkMock
            .Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _userLibraryService.AddAsync(userLibrary);
        await act.Should().ThrowAsync<Exception>().WithMessage("Commit exception");

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
