using TecChallenge.Application.Controllers;
using TecChallenge.Domain.Interfaces;

namespace TecChallenge.Application.V1.Controllers;

public class GameController(
    INotifier notifier,
    IUser appUser,
    ILocalizationRecordRepository localizationRecordRepository,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment)
    : MainController(notifier, appUser, localizationRecordRepository, httpContextAccessor, webHostEnvironment)
{
    
}