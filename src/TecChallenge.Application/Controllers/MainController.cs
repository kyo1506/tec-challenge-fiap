using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TecChallenge.Domain.Entities;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Domain.Notifications;
using TecChallenge.Shared.Models.Generics;
using IUser = TecChallenge.Domain.Interfaces.IUser;

namespace TecChallenge.Application.Controllers;

[ApiController]
public abstract class MainController : ControllerBase
{
    private readonly IUser _appUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalizationRecordRepository _localizationRecordRepository;
    private readonly INotifier _notifier;
    protected readonly IWebHostEnvironment WebHostEnvironment;

    protected MainController(
        INotifier notifier,
        IUser appUser,
        ILocalizationRecordRepository localizationRecordRepository,
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment webHostEnvironment
    )
    {
        _notifier = notifier;
        _appUser = appUser;

        if (appUser.IsAuthenticated())
        {
            UserId = appUser.GetUserId();
            AuthenticatedUser = true;
        }

        _localizationRecordRepository = localizationRecordRepository;
        _httpContextAccessor = httpContextAccessor;
        WebHostEnvironment = webHostEnvironment;
    }

    protected Guid UserId { get; set; }
    protected bool AuthenticatedUser { get; set; }

    private bool OperationValid()
    {
        return !_notifier.HasNotification();
    }

    protected ActionResult<Root<T>> CustomResponse<T>(
        T? data = null,
        HttpStatusCode statusCode = HttpStatusCode.OK
    ) where T : class
    {
        return OperationValid() ? SuccessResponse(data, statusCode) : ErrorResponse<T>(statusCode);
    }

    protected ActionResult<Root<T>> CustomModelStateResponse<T>(
        ModelStateDictionary modelState,
        HttpStatusCode statusCode = HttpStatusCode.OK
    ) where T : class
    {
        if (!modelState.IsValid) NotifyErrorInvalidModel(modelState);
        return CustomResponse<T>(statusCode: statusCode);
    }

    private ActionResult<Root<T>> SuccessResponse<T>(
        T? data,
        HttpStatusCode statusCode = HttpStatusCode.OK
    ) where T : class
    {
        if ((int)statusCode == StatusCodes.Status204NoContent) return NoContent();

        return new Root<T>
        {
            StatusCode = (int)statusCode,
            Success = true,
            Data = data
        };
    }

    private ActionResult<Root<T>> ErrorResponse<T>(
        HttpStatusCode statusCode = HttpStatusCode.BadRequest
    ) where T : class
    {
        var errors = _notifier.GetNotifications().Select(n => n.Message).ToArray();

        return new Root<T>
        {
            StatusCode = (int)statusCode,
            Success = false,
            Errors = errors
        };
    }

    private void NotifyErrorInvalidModel(ModelStateDictionary modelState)
    {
        var errors = modelState.Values.SelectMany(e => e.Errors);
        foreach (var error in errors)
        {
            var errorMsg = error.Exception == null ? error.ErrorMessage : error.Exception.Message;
            NotifyError(errorMsg);
        }
    }

    protected void NotifyError(string message)
    {
        _notifier.Handle(new Notification(message));
    }

    protected async Task<IEnumerable<LocalizationRecord>?> GetLocalizedStrings(
        string resourceKey
    )
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var userCulture = "en-US";

        if (string.IsNullOrEmpty(request?.Headers.AcceptLanguage))
            return await _localizationRecordRepository.SearchListAsync(c =>
                c.ResourceKey == resourceKey && c.LocalizationCulture == userCulture
            );

        var acceptLanguage = request.Headers.AcceptLanguage.ToString();
        var cultures = acceptLanguage
            .Split(',')
            .Select(lang => lang.Split(';').FirstOrDefault())
            .ToList();

        foreach (var culture in cultures)
        {
            userCulture = CultureInfo.GetCultureInfo(culture.Trim()).Name;
            break;
        }

        // Busca as strings localizadas com base na cultura do usuário
        return await _localizationRecordRepository.SearchListAsync(c =>
            c.ResourceKey == resourceKey && c.LocalizationCulture == userCulture
        );
    }

    protected async Task<IEnumerable<LocalizationRecord>?> GetLocalizedStrings(
        string resourceKey,
        string key
    )
    {
        return await _localizationRecordRepository.SearchListAsync(c =>
            c.ResourceKey == resourceKey && c.Key.StartsWith(key)
        );
    }

    protected async Task<string> GetTemplateFile()
    {
        var filePath = Path.Combine(
            WebHostEnvironment.WebRootPath,
            "assets",
            "templates",
            "template.html"
        );

        return await System.IO.File.ReadAllTextAsync(filePath);
    }

    protected bool HasClaim(string claimName, string claimValue)
    {
        return _appUser.IsAuthenticated()
               && _appUser
                   .GetClaimsIdentity()
                   .Any(c => c.Type == claimName && c.Value.Contains(claimValue));
    }
}