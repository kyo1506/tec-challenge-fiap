using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Web;
using Asp.Versioning;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TecChallenge.Application.Controllers;
using TecChallenge.Application.Extensions;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Shared.Models.DTOs;
using TecChallenge.Shared.Models.Generics;

namespace TecChallenge.Application.V1.Controllers;

[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthController(
    INotifier notifier,
    IUser appUser,
    ILocalizationRecordRepository localizationRecordRepository,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment webHostEnvironment,
    ApplicationSignInManager applicationSignInManager,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<JwtOptions> jwtOptions,
    IOptions<UrlConfiguration> urlConfiguration,
    IEmailService emailService)
    : MainController(notifier, appUser, localizationRecordRepository, httpContextAccessor, webHostEnvironment)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly UrlConfiguration _urlConfiguration = urlConfiguration.Value;

    /// <summary>
    /// Get all users
    /// </summary>
    /// <returns>User collection</returns>
    /// <response code="200">Success</response>
    /// <response code="403">Access denied</response>
    [HttpGet]
    [ClaimsAuthorize("Auth", "GetAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<Root<IEnumerable<UserDto>>> GetAllUsers()
    {
        var listUserViewModel = userManager
            .Users.ToAsyncEnumerable()
            .SelectAwait(async user => new UserDto()
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Role = (await GetRoleNameAsync(user)),
                IsDeleted = user.IsDeleted,
                FirstAccess = user.FirstAccess,
            })
            .ToEnumerable();

        return CustomResponse(listUserViewModel);
    }

    /// <summary>
    /// Get user and their claims
    /// </summary>
    /// <param name="id">User identifier</param>
    /// <returns>User data</returns>
    /// <response code="200">Success</response>
    /// <response code="403">Access denied</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id:guid}")]
    [ClaimsAuthorize("Auth", "GetById")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<UserDto>>> GetUserById(Guid id)
    {
        var user = await FindUserByIdAsync(id);

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<UserDto>(null, HttpStatusCode.NotFound);
        }

        var userClaims = (await userManager.GetClaimsAsync(user)).Select(claim => new ClaimDto
            { Type = claim.Type, Value = claim.Value }
        );

        var model = new UserDto
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            Role = await GetRoleNameAsync(user),
            IsDeleted = user.IsDeleted,
            FirstAccess = user.FirstAccess,
            UserClaims = userClaims
        };

        return CustomResponse(model);
    }

    /// <summary>
    /// Send a password reset link to the email provided, if the username is valid
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>Sends an email with a password reset link to the user</returns>
    /// <response code="200">Success</response>
    /// <response code="400">It was not possible to send the email</response>
    /// <response code="404">User not found</response>
    [HttpGet("reset-password/{email}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<string>>> ResetPassword(string email)
    {
        var user = await FindUserByEmailAsync(email);

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<string>(statusCode: HttpStatusCode.NotFound);
        }

        var token = HttpUtility.UrlEncode(
            await userManager.GeneratePasswordResetTokenAsync(user)
        );

        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var passwordResetUrl =
            $"{_urlConfiguration.UrlPortal}/auth/reset-password/validate?email="
            + email
            + "&token="
            + token;

        var template = await GetTemplateFile();

        var notification = localizedStrings?.FirstOrDefault(x => x.Key == "Notification")?.Text;
        var message = localizedStrings
            ?.FirstOrDefault(x => x.Key == "SendLinkPasswordReset")
            ?.Text;
        var title = localizedStrings?.FirstOrDefault(x => x.Key == "TitlePasswordReset")?.Text;
        var successMessage = localizedStrings
            ?.FirstOrDefault(x => x.Key == "SentPasswordReset")
            ?.Text;

        var failMessage = localizedStrings?.FirstOrDefault(x => x.Key == "FailEmail")?.Text;

        template = template
            .Replace("[NOTIFICATION]", notification)
            .Replace("[MESSAGE]", message?.Replace("[LINK]", passwordResetUrl))
            .Replace("[YEAR]", DateTime.UtcNow.Year.ToString());

        var resultEmail = await emailService.SendAsync(
            subject: title ?? string.Empty,
            body: template,
            recipient: user.Email,
            env: WebHostEnvironment.EnvironmentName
        );

        if (resultEmail) return CustomResponse(successMessage);

        NotifyError(failMessage ?? string.Empty);
        return CustomResponse<string>();
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    /// <remarks>
    /// The information received by the user's email must be used in the request
    /// The password must contain at least 8 digits, an uppercase letter, a lowercase letter, a number and a special character
    ///
    ///     POST /api/v1/auth/reset-password
    ///     {
    ///       "token": "string",
    ///       "email": "user@example.com",
    ///       "password": "string",
    ///       "confirmPassword": "string"
    ///     }
    ///
    /// </remarks>
    /// <param name="model">User data regarding password reset</param>
    /// <returns>Returns an email notifying you that the password has been successfully reset</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Failed to send email confirming password reset or failed to reset user password</response>
    /// <response code="404">User not found</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<string>>> ResetPassword(ChangePasswordDto model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<string>(ModelState);

        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var user = await FindUserByEmailAsync(model.Email);

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<string>(null, HttpStatusCode.NotFound);
        }

        var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (result.Succeeded)
        {
            var template = await GetTemplateFile();

            var notification = localizedStrings
                ?.FirstOrDefault(x => x.Key == "Notification")
                ?.Text;
            var message = localizedStrings
                ?.FirstOrDefault(x => x.Key == "UserPasswordReset")
                ?.Text;
            var title = localizedStrings
                ?.FirstOrDefault(x => x.Key == "TitlePasswordReset")
                ?.Text;
            var failMessage = localizedStrings?.FirstOrDefault(x => x.Key == "FailEmail")?.Text;

            template = template
                .Replace("[NOTIFICATION]", notification)
                .Replace("[MESSAGE]", message)
                .Replace("[YEAR]", DateTime.UtcNow.Year.ToString());

            var resultEmail = await emailService.SendAsync(
                subject: title ?? string.Empty,
                body: template,
                recipient: user.Email,
                env: WebHostEnvironment.EnvironmentName
            );

            if (resultEmail) return CustomResponse(message);

            NotifyError(failMessage ?? "Failed to send email");
            return CustomResponse<string>();
        }

        result.Errors.ToList().ForEach(error => NotifyError(error.Description));
        return CustomResponse<string>();
    }

    /// <summary>
    /// Send a confirmation link to the email provided if the username is valid
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>Sends an email with a confirmation link to the user</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Unable to send already confirmed email or user</response>
    /// <response code="404">User not found</response>
    [HttpGet("confirm-email/{email}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<string>>> ConfirmEmail(string email)
    {
        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var user = await FindUserByEmailAsync(email);

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<string>(null, HttpStatusCode.NotFound);
        }

        if (user.EmailConfirmed)
        {
            NotifyError(localizedStrings?.FirstOrDefault(x => x.Key == "UserEmailConfirmed")?.Text);
            return CustomResponse<string>();
        }

        var token = HttpUtility.UrlEncode(
            await userManager.GenerateEmailConfirmationTokenAsync(user)
        );

        var confirmEmailUrl =
            $"{_urlConfiguration.UrlPortal}/auth/confirm-email/validate?email="
            + email
            + "&token="
            + token;

        var template = await GetTemplateFile();

        var notification = localizedStrings?.FirstOrDefault(x => x.Key == "Notification")?.Text;
        var message = localizedStrings
            ?.FirstOrDefault(x => x.Key == "SendLinkEmailConfirmation")
            ?.Text;
        var title = localizedStrings
            ?.FirstOrDefault(x => x.Key == "TitleEmailConfirmation")
            ?.Text;
        var successMessage = localizedStrings
            ?.FirstOrDefault(x => x.Key == "SentEmailConfirmation")
            ?.Text;

        var failMessage = localizedStrings?.FirstOrDefault(x => x.Key == "FailEmail")?.Text;

        template = template
            .Replace("[NOTIFICATION]", notification)
            .Replace("[MESSAGE]", message?.Replace("[LINK]", confirmEmailUrl))
            .Replace("[YEAR]", DateTime.UtcNow.Year.ToString());

        var resultEmail = await emailService.SendAsync(
            subject: title ?? string.Empty,
            body: template,
            recipient: user.Email,
            env: WebHostEnvironment.EnvironmentName
        );

        if (resultEmail) return CustomResponse(successMessage);

        NotifyError(failMessage ?? string.Empty);
        return CustomResponse<string>();
    }

    /// <summary>
    /// Confirm user email
    /// </summary>
    /// <param name="model">User Information</param>
    /// <returns>Returns an email validating email confirmation</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Failed to send email or failed to confirm user</response>
    /// <response code="404">User not found</response>
    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<string>>> ConfirmEmail(ConfirmEmailDto model)
    {
        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var user = await FindUserByEmailAsync(model.Email);

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<string>(statusCode: HttpStatusCode.NotFound);
        }

        if (user.EmailConfirmed)
        {
            NotifyError(localizedStrings?.FirstOrDefault(x => x.Key == "UserEmailConfirmed")?.Text);
            return CustomResponse<string>();
        }

        var result = await userManager.ConfirmEmailAsync(user, model.Token);

        if (result.Succeeded)
        {
            var template = await GetTemplateFile();

            var notification = localizedStrings
                ?.FirstOrDefault(x => x.Key == "Notification")
                ?.Text;
            var message = localizedStrings
                ?.FirstOrDefault(x => x.Key == "SuccessConfirmEmail")
                ?.Text;
            var title = localizedStrings
                ?.FirstOrDefault(x => x.Key == "TitleEmailConfirmation")
                ?.Text;
            var successMessage = localizedStrings
                ?.FirstOrDefault(x => x.Key == "SuccessConfirmEmail")
                ?.Text;
            var failMessage = localizedStrings?.FirstOrDefault(x => x.Key == "FailEmail")?.Text;

            template = template
                .Replace("[NOTIFICATION]", notification)
                .Replace("[MESSAGE]", message)
                .Replace("[YEAR]", DateTime.UtcNow.Year.ToString());

            var resultEmail = await emailService.SendAsync(
                subject: title ?? string.Empty,
                body: template,
                recipient: user.Email,
                env: WebHostEnvironment.EnvironmentName
            );

            if (resultEmail) return CustomResponse(successMessage);

            NotifyError(failMessage ?? string.Empty);
            return CustomResponse<string>();
        }

        result.Errors?.ToList().ForEach(error => NotifyError(error.Description));
        return CustomResponse<string>();
    }

    /// <summary>
    /// Register a new account
    /// </summary>
    /// <remarks>
    /// It will only be possible to register an account if the user is logged in and the email must be valid
    ///
    ///     POST /api/v1/auth/register
    ///     {
    ///         "email": "teste@atento.com",
    ///         "roleName": "Administrador",
    ///         "userClaims": [
    ///             {
    ///                 "type": "VIVO",
    ///                 "value": "Quebra de Sigilo,Pagamentos Judiciais"
    ///             }
    ///         ]
    ///     }
    ///
    /// </remarks>
    /// <param name="model">User creation data</param>
    /// <returns>Object created and an email with the password automatically generated for the user</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Badly formatted object or error during account creation</response>
    /// <response code="403">Access denied</response>
    [HttpPost("register")]
    [ClaimsAuthorize("Auth", "Add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<string>>> AddUser(CreateUserDto model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<string>(ModelState);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            IsDeleted = false,
            FirstAccess = true,
        };

        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var password = GenerateRandomPassword();

        var claims = model.UserClaims.Select(claim => new Claim(claim.Type, claim.Value));

        var resultUser = await userManager.CreateAsync(user, password);

        if (resultUser.Succeeded)
        {
            var resultRole = await userManager.AddToRoleAsync(user, model.Role);

            var resultUserClaims = await userManager.AddClaimsAsync(user, claims);

            if (resultRole.Succeeded && resultUserClaims.Succeeded)
            {
                var template = await GetTemplateFile();

                var notification = localizedStrings
                    ?.FirstOrDefault(x => x.Key == "Notification")
                    ?.Text;
                var message = localizedStrings
                    ?.FirstOrDefault(x => x.Key == "UserNewAccount")
                    ?.Text;
                var title = localizedStrings
                    ?.FirstOrDefault(x => x.Key == "TitleUserNewAccount")
                    ?.Text;
                var successMessage = localizedStrings
                    ?.FirstOrDefault(x => x.Key == "SentUserNewAcoount")
                    ?.Text;

                var failMessage = localizedStrings
                    ?.FirstOrDefault(x => x.Key == "FailEmail")
                    ?.Text;

                template = template
                    .Replace("[NOTIFICATION]", notification)
                    .Replace("[MESSAGE]", message?.Replace("password", password))
                    .Replace("[YEAR]", DateTime.UtcNow.Year.ToString());

                var resultEmail = await emailService.SendAsync(
                    subject: title ?? string.Empty,
                    body: template,
                    recipient: user.Email,
                    env: WebHostEnvironment.EnvironmentName
                );

                if (resultEmail) return CustomResponse(successMessage);

                NotifyError(failMessage ?? string.Empty);
                return CustomResponse<string>();
            }

            resultRole.Errors?.ToList().ForEach(error => NotifyError(error.Description));
            resultUserClaims.Errors?.ToList().ForEach(error => NotifyError(error.Description));
        }

        resultUser.Errors.ToList().ForEach(error => NotifyError(error.Description));
        return CustomResponse<string>();
    }

    /// <summary>
    /// Log in to the application
    /// </summary>
    /// <param name="model">User data</param>
    /// <returns>Access Token, Refresh Token and user information</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Object poorly formatted or access not permitted</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<LoginResponseDto>>> Login(LoginDto model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<LoginResponseDto>(ModelState);

        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var userNotAllowed = localizedStrings
            ?.FirstOrDefault(x => x.Key == "UserNotAllowed")
            ?.Text;
        var userBlocked = localizedStrings?.FirstOrDefault(x => x.Key == "UserBlocked")?.Text;
        var userInvalid = localizedStrings?.FirstOrDefault(x => x.Key == "UserInvalid")?.Text;

        var result = await applicationSignInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            false,
            true
        );

        if (result.Succeeded) return CustomResponse(await GenerateCredentialsAsync(model.Email));

        string error;

        if (result.IsNotAllowed)
        {
            error = userNotAllowed ?? "User not allowed. Please contact those responsible";
        }
        else if (result.IsLockedOut)
        {
            error = userBlocked ?? "User temporarily blocked due to invalid attempts";
        }
        else
        {
            error = userInvalid ?? "Incorrect username and/or password";
        }

        NotifyError(error);
        return CustomResponse<LoginResponseDto>();
    }

    /// <summary>
    /// Renew the Access Token based on the Refresh Token
    /// </summary>
    /// <param name="model">Refresh Token</param>
    /// <returns>Access Token, Refresh Token and user information</returns>
    /// <response code="200">Success</response>
    /// <response code="400">If not, the token is not valid, is empty or access is not permitted for the user</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Root<LoginResponseDto>>> RefreshSession(RefreshTokenViewModel model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<LoginResponseDto>(ModelState);

        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var invalidToken = localizedStrings?.FirstOrDefault(x => x.Key == "InvalidToken")?.Text;
        var userLockedOut = localizedStrings
            ?.FirstOrDefault(x => x.Key == "UserLockedOut")
            ?.Text;

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = GetValidationParameters();

        var validatedToken = await tokenHandler.ValidateTokenAsync(
            model.RefreshToken,
            validationParameters
        );

        if (!validatedToken.IsValid)
        {
            NotifyError(invalidToken ?? "Invalid token");
            return CustomResponse<LoginResponseDto>();
        }

        var user = await userManager.FindByIdAsync(
            validatedToken.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value
        );

        if (!await userManager.IsLockedOutAsync(user))
            return CustomResponse(await GenerateCredentialsAsync(user.Email));

        NotifyError(userLockedOut ?? "User is blocked");
        return CustomResponse<LoginResponseDto>();
    }

    /// <summary>
    /// Reset user password on first access
    /// </summary>
    /// <remarks>
    /// The password must contain at least 8 digits, an uppercase letter, a lowercase letter, a number and a special character
    ///
    ///     POST /api/v1/auth/first-access
    ///     {
    ///       "token": null,
    ///       "email": "user@example.com",
    ///       "password": "string",
    ///       "confirmPassword": "string"
    ///     }
    ///
    /// </remarks>
    /// <param name="model">User data regarding password reset</param>
    /// <returns>Returns an email with the account confirmation link</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Failed to send email or failed to reset user password</response>
    /// <response code="404">User not found</response>
    [HttpPost("first-access")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<string>>> FirstAccess(ChangePasswordDto model)
    {
        if (!ModelState.IsValid) return CustomModelStateResponse<string>(ModelState);

        var user = await FindUserByEmailAsync(model.Email);

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<string>(statusCode: HttpStatusCode.NotFound);
        }

        var localizedStrings = await GetLocalizedStrings(nameof(AuthController));

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var result = await userManager.ResetPasswordAsync(user, token, model.Password);

        if (result.Succeeded)
        {
            user.FirstAccess = false;

            await userManager.UpdateAsync(user);

            var template = await GetTemplateFile();

            token = HttpUtility.UrlEncode(
                await userManager.GenerateEmailConfirmationTokenAsync(user)
            );

            var notification = localizedStrings
                ?.FirstOrDefault(x => x.Key == "Notification")
                ?.Text;
            var message = localizedStrings
                ?.FirstOrDefault(x => x.Key == "SendLinkEmailConfirmation")
                ?.Text;
            var title = localizedStrings
                ?.FirstOrDefault(x => x.Key == "TitleEmailConfirmation")
                ?.Text;
            var successMessage = localizedStrings
                ?.FirstOrDefault(x => x.Key == "SentEmailConfirmation")
                ?.Text;
            var failMessage = localizedStrings?.FirstOrDefault(x => x.Key == "FailEmail")?.Text;

            var confirmEmailUrl =
                $"{_urlConfiguration.UrlPortal}/auth/confirm-email/validate?email="
                + user.Email
                + "&token="
                + token;

            template = template
                .Replace("[NOTIFICATION]", notification)
                .Replace("[MESSAGE]", message?.Replace("[LINK]", confirmEmailUrl))
                .Replace("[YEAR]", DateTime.UtcNow.Year.ToString());

            var resultEmail = await emailService.SendAsync(
                subject: title ?? string.Empty,
                body: template,
                recipient: user.Email,
                env: WebHostEnvironment.EnvironmentName
            );

            if (resultEmail) return CustomResponse(successMessage);

            NotifyError(failMessage ?? string.Empty);
            return CustomResponse<string>();
        }

        result.Errors?.ToList().ForEach(error => NotifyError(error.Description));
        return CustomResponse<string>();
    }

    /// <summary>
    /// Update a user
    /// </summary>
    /// <remarks>
    /// It is not possible to leave the value field of UserClaims empty, as this will break the operation access validation logic
    ///
    ///     PUT /api/v1/auth/{id:guid}
    ///     {
    ///       "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///       "username": "string",
    ///       "email": "user@example.com",
    ///       "roleName": "string",
    ///       "firstAccess": true,
    ///       "isDeleted": true,
    ///       "userClaims": [
    ///         {
    ///           "type": "string",
    ///           "value": "string"
    ///         }
    ///       ]
    ///     }
    ///
    /// </remarks>
    /// <param name="id">User identification</param>
    /// <param name="model">User information to be updated</param>
    /// <returns>Returns the updated object</returns>
    /// <response code="200">Success</response>
    /// <response code="403">Access denied</response>
    /// <response code="400">Poorly formatted object or failure to update user information</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id:guid}")]
    [ClaimsAuthorize("Auth", "Update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<string>>> UpdateUser(Guid id, UserDto model)
    {
        if (id != model.Id)
        {
            NotifyError("The ids provided are not the same");
            return CustomResponse<string>();
        }

        if (!ModelState.IsValid) return CustomModelStateResponse<string>(ModelState);

        var user = await FindUserByIdAsync(id);

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<string>(null, HttpStatusCode.NotFound);
        }

        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault();

        if (role != model.Role)
        {
            await userManager.RemoveFromRoleAsync(user, role);
            await userManager.AddToRoleAsync(user, model.Role);
        }

        user.UserName = model.Username;
        user.Email = model.Email;
        user.IsDeleted = model.IsDeleted;

        var resultUser = await userManager.UpdateAsync(user);

        if (!resultUser.Succeeded)
        {
            resultUser.Errors?.ToList().ForEach(x => NotifyError(x.Description));
        }

        var oldClaims = (await userManager.GetClaimsAsync(user)).ToList();
        var newClaims = model
            .UserClaims.Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();

        foreach (var newClaim in newClaims)
        {
            var replaceClaim = oldClaims.FirstOrDefault(claim =>
                claim.Type.Equals(newClaim.Type) && claim.Value != newClaim.Value
            );

            if (replaceClaim is not null)
            {
                await userManager.RemoveClaimAsync(user, replaceClaim);
                await userManager.AddClaimAsync(user, newClaim);
            }

            var addClaim = oldClaims.FirstOrDefault(claim => claim.Type.Equals(newClaim.Type));

            if (addClaim is null)
            {
                await userManager.AddClaimAsync(user, newClaim);
            }
        }

        foreach (var oldClaim in from oldClaim in oldClaims
                 let removeClaim = newClaims.FirstOrDefault(claim =>
                     claim.Type.Equals(oldClaim.Type)
                 )
                 where removeClaim is null
                 select oldClaim)
        {
            await userManager.RemoveClaimAsync(user, oldClaim);
        }

        return CustomResponse<string>(statusCode:HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="id">User identification</param>
    /// <returns>User deleted</returns>
    /// <response code="200">Success</response>
    /// <response code="403">Access denied</response>
    /// <response code="400">Failed to delete user</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id:guid}")]
    [ClaimsAuthorize("Auth", "Delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Root<string>>> DeleteUser(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            NotifyError("User not found");
            return CustomResponse<string>(null, HttpStatusCode.NotFound);
        }

        user.IsDeleted = true;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded) result.Errors.ToList().ForEach(x => NotifyError(x.Description));

        return CustomResponse<string>(statusCode: HttpStatusCode.NoContent);
    }

    private async Task<LoginResponseDto> GenerateCredentialsAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        var role = await roleManager.FindByNameAsync((await GetRoleNameAsync(user)));

        var userClaims = await userManager.GetClaimsAsync(user);
        var roleClaims = await roleManager.GetClaimsAsync(role);

        var expirationDateAccessToken = DateTime.UtcNow.AddSeconds(
            _jwtOptions.AccessTokenExpiration
        );
        var expirationDateRefreshToken = DateTime.UtcNow.AddSeconds(
            _jwtOptions.RefreshTokenExpiration
        );

        var accessToken = GenerateToken(user, expirationDateAccessToken, roleClaims);
        var refreshToken = GenerateToken(user, expirationDateRefreshToken);

        return new LoginResponseDto()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserToken = new UserTokenDto
            {
                Id = user.Id,
                Email = email,
                RoleClaims = roleClaims.Select(c => new ClaimDto()
                {
                    Type = c.Type,
                    Value = c.Value,
                }),
                UserClaims = userClaims.Select(c => new ClaimDto()
                {
                    Type = c.Type,
                    Value = c.Value,
                }),
                UserConfig =
                [
                    new ClaimDto { Type = "Role", Value = role.Name },
                    new ClaimDto { Type = "Level", Value = role.Level.ToString() },
                    new ClaimDto { Type = "FirstAccess", Value = user.FirstAccess.ToString() },
                    new ClaimDto { Type = "EmailConfirmed", Value = user.EmailConfirmed.ToString() },
                ],
            },
        };
    }

    private string GenerateToken(
        ApplicationUser user,
        DateTime expirationDate,
        IList<Claim>? roleClaims = null
    )
    {
        var defaultClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                ToUnixEpochDate(DateTime.UtcNow).ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: roleClaims != null && roleClaims.Any()
                ? defaultClaims.Concat(roleClaims)
                : defaultClaims,
            notBefore: DateTime.UtcNow,
            expires: expirationDate,
            signingCredentials: _jwtOptions.SigningCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private TokenValidationParameters GetValidationParameters() =>
        new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _jwtOptions.SecurityKey,
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero,
        };

    private async Task<ApplicationUser?> FindUserByEmailAsync(string email) =>
        await userManager.FindByEmailAsync(email);

    private async Task<ApplicationUser?> FindUserByIdAsync(Guid id) =>
        await userManager.FindByIdAsync(id.ToString());

    private async Task<string?> GetRoleNameAsync(ApplicationUser user) =>
        (await userManager.GetRolesAsync(user)).FirstOrDefault();

    private static long ToUnixEpochDate(DateTime date) =>
        (long)
        Math.Round(
            (
                date.ToUniversalTime()
                - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)
            ).TotalSeconds
        );

    private static string GenerateRandomPassword(PasswordOptions? opts = null)
    {
        opts ??= new PasswordOptions()
        {
            RequiredLength = 8,
            RequiredUniqueChars = 4,
            RequireDigit = true,
            RequireLowercase = true,
            RequireNonAlphanumeric = true,
            RequireUppercase = true,
        };

        string[] randomChars =
        [
            "ABCDEFGHJKLMNOPQRSTUVWXYZ",
            "abcdefghijkmnopqrstuvwxyz",
            "0123456789",
            "!@$?_-",
        ];

        CryptoRandom rand = new();
        List<char> chars = [];

        if (opts.RequireUppercase)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[0][rand.Next(0, randomChars[0].Length)]
            );

        if (opts.RequireLowercase)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[1][rand.Next(0, randomChars[1].Length)]
            );

        if (opts.RequireDigit)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[2][rand.Next(0, randomChars[2].Length)]
            );

        if (opts.RequireNonAlphanumeric)
            chars.Insert(
                rand.Next(0, chars.Count),
                randomChars[3][rand.Next(0, randomChars[3].Length)]
            );

        for (
            var i = chars.Count;
            i < opts.RequiredLength || chars.Distinct().Count() < opts.RequiredUniqueChars;
            i++
        )
        {
            var rcs = randomChars[rand.Next(0, randomChars.Length)];
            chars.Insert(rand.Next(0, chars.Count), rcs[rand.Next(0, rcs.Length)]);
        }

        return new string([.. chars]);
    }
}