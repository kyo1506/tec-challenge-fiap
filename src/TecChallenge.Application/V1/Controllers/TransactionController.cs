using System.Net;
using Microsoft.AspNetCore.Mvc;
using TecChallenge.Domain.Exceptions;
using TecChallenge.Shared.Models.Dtos.Requests;
using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Application.V1.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/transactions")]
[Produces("application/json")]
public class TransactionController(
    INotifier notifier,
    IUser appUser,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    ITransactionService transactionService
) : MainController(notifier, appUser, httpContextAccessor, webHostEnvironment)
{
    /// <summary>
    /// Process a game purchase transaction
    /// </summary>
    /// <param name="request">Purchase request containing user ID, game ID, and optional promotion ID</param>
    /// <returns>Purchase confirmation with transaction details</returns>
    /// <response code="200">Purchase processed successfully</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <exception cref="PromotionNotApplicableException">When promotion cannot be applied</exception>
    /// <exception cref="InsufficientBalanceException">When user has insufficient balance</exception>
    /// <exception cref="DomainException">General domain validation error</exception>
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(Root<PurchaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<PurchaseResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<PurchaseResponse>>> PurchaseGame(
        PurchaseGameRequest request
    )
    {
        try
        {
            if (!ModelState.IsValid)
                return CustomModelStateResponse<PurchaseResponse>(ModelState);

            return CustomResponse(
                await transactionService.ProcessPurchaseAsync(
                    userId: request.UserId,
                    gameId: request.GameId,
                    promotionGameId: request.PromotionGameId
                )
            );
        }
        catch (Exception e)
            when (e
                    is PromotionNotApplicableException
                        or InsufficientBalanceException
                        or DomainException
            )
        {
            NotifyError(e.Message);
            return CustomResponse<PurchaseResponse>(statusCode: HttpStatusCode.BadRequest);
        }
    }

    /// <summary>
    /// Process a purchase refund
    /// </summary>
    /// <param name="request">Refund request containing user ID and game ID</param>
    /// <returns>Refund confirmation with transaction details</returns>
    /// <response code="200">Refund processed successfully</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <exception cref="DomainException">General domain validation error</exception>
    [HttpPut("refund-purchase")]
    [ProducesResponseType(typeof(Root<RefundResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<RefundResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<RefundResponse>>> RefundedPurchase(
        RefundPurchaseRequest request
    )
    {
        try
        {
            if (!ModelState.IsValid)
                return CustomModelStateResponse<RefundResponse>(ModelState);

            return CustomResponse(
                await transactionService.RefundedPurchaseAsync(
                    userId: request.UserId,
                    gameId: request.GameId
                )
            );
        }
        catch (Exception e) when (e is DomainException)
        {
            NotifyError(e.Message);
            return CustomResponse<RefundResponse>(statusCode: HttpStatusCode.BadRequest);
        }
    }

    /// <summary>
    /// Deposit funds into user's account
    /// </summary>
    /// <param name="request">Deposit request containing user ID and amount</param>
    /// <returns>Deposit confirmation with new balance</returns>
    /// <response code="200">Deposit processed successfully</response>
    /// <response code="400">Invalid request data or business rule violation</response>
    /// <exception cref="DomainException">General domain validation error</exception>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(Root<DepositResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<DepositResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<DepositResponse>>> Deposit(BalanceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return CustomModelStateResponse<DepositResponse>(ModelState);

            return CustomResponse(
                await transactionService.DepositAsync(
                    userId: request.UserId,
                    amount: request.Amount
                )
            );
        }
        catch (Exception e) when (e is DomainException)
        {
            NotifyError(e.Message);
            return CustomResponse<DepositResponse>(statusCode: HttpStatusCode.BadRequest);
        }
    }

    /// <summary>
    /// Withdraw funds from user's account
    /// </summary>
    /// <param name="request">Withdrawal request containing user ID and amount</param>
    /// <returns>Withdrawal confirmation with new balance</returns>
    /// <response code="200">Withdrawal processed successfully</response>
    /// <response code="400">Invalid request data or insufficient funds</response>
    /// <exception cref="InsufficientBalanceException">When user has insufficient balance</exception>
    /// <exception cref="DomainException">General domain validation error</exception>
    [HttpPut("withdraw")]
    [ProducesResponseType(typeof(Root<WithdrawalResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Root<WithdrawalResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<WithdrawalResponse>>> Withdraw(BalanceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return CustomModelStateResponse<WithdrawalResponse>(ModelState);

            return CustomResponse(
                await transactionService.WithdrawalAsync(
                    userId: request.UserId,
                    amount: request.Amount
                )
            );
        }
        catch (Exception e) when (e is InsufficientBalanceException or DomainException)
        {
            NotifyError(e.Message);
            return CustomResponse<WithdrawalResponse>(statusCode: HttpStatusCode.BadRequest);
        }
    }
}
