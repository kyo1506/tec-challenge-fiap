using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TecChallenge.Application.Extensions;

public class ApplicationSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation
)
    : SignInManager<ApplicationUser>(
        userManager,
        contextAccessor,
        claimsFactory,
        optionsAccessor,
        logger,
        schemes,
        confirmation
    )
{
    public override async Task<SignInResult> PasswordSignInAsync(
        ApplicationUser user,
        string password,
        bool isPersistent,
        bool lockoutOnFailure
    )
    {
        if (user.IsDeleted)
            return SignInResult.NotAllowed;

        var attempt = await CheckPasswordSignInAsync(user, password, lockoutOnFailure);

        return attempt.Succeeded ? await SignInOrTwoFactorAsync(user, isPersistent) : attempt;
    }
}