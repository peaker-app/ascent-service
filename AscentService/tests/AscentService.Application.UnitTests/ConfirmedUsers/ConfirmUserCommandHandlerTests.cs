using AscentService.Application.ConfirmedUsers.ConfirmUser;
using AscentService.Domain.ConfirmedUsers;
using Common.Application.Abstractions;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.ConfirmedUsers;

public sealed class ConfirmUserCommandHandlerTests
{
    private static readonly DateTime ConfirmedAt = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IConfirmedUserRepository _repository = Substitute.For<IConfirmedUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ConfirmUserCommandHandler _handler;

    public ConfirmUserCommandHandlerTests() => _handler = new ConfirmUserCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task Handle_ForANewUser_ProjectsTheConfirmation()
    {
        Guid userId = Guid.CreateVersion7();
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((ConfirmedUser?)null);

        Result result = await _handler.Handle(new ConfirmUserCommand(userId, ConfirmedAt), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.Received(1).Add(Arg.Is<ConfirmedUser>(confirmed => confirmed!.Id == userId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForAnAlreadyProjectedUser_DoesNotWriteAgain()
    {
        Guid userId = Guid.CreateVersion7();
        _repository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(ConfirmedUser.Project(userId, ConfirmedAt));

        Result result = await _handler.Handle(new ConfirmUserCommand(userId, ConfirmedAt), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repository.DidNotReceive().Add(Arg.Any<ConfirmedUser>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
