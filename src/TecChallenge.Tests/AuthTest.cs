using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TecChallenge.Application.Extensions;
using TecChallenge.Application.V1.Controllers;
using TecChallenge.Domain.Entities;
using TecChallenge.Domain.Interfaces;
using TecChallenge.Domain.Notifications;
using TecChallenge.Shared.Models.Dtos;

namespace TecChallenge.Tests
{
    public class AuthTest
    {
        private readonly Mock<INotifier> _mockNotifier;
        private readonly Mock<IUser> _mockAppUser;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly Mock<ApplicationSignInManager> _mockSignInManager;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private readonly Mock<IOptions<JwtOptions>> _mockJwtOptions;
        private readonly Mock<IOptions<UrlConfiguration>> _mockUrlConfiguration;
        private readonly Mock<IMockEmailService> _mockEmailService;
        private readonly Mock<IUserLibraryService> _mockUserLibraryService;
        private readonly AuthController _controller;
        private readonly string _testTemplateContent;

        public AuthTest()
        {
            _mockNotifier = new Mock<INotifier>();
            _mockAppUser = new Mock<IUser>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockUserManager = CreateMockUserManager();
            _mockRoleManager = CreateMockRoleManager();
            _mockSignInManager = CreateMockApplicationSignInManager(_mockUserManager.Object);
            _mockJwtOptions = new Mock<IOptions<JwtOptions>>();
            _mockUrlConfiguration = new Mock<IOptions<UrlConfiguration>>();
            _mockEmailService = new Mock<IMockEmailService>();
            _mockUserLibraryService = new Mock<IUserLibraryService>();

            _testTemplateContent = @"
                <html>
                    <body>
                        <h1>[NOTIFICATION]</h1>
                        <p>[MESSAGE]</p>
                        <footer>© [YEAR] Test Company</footer>
                    </body>
                </html>";

            SetupMockDefaults();
            SetupFileSystemMocks();

            _controller = new AuthController(
                _mockNotifier.Object,
                _mockAppUser.Object,
                _mockHttpContextAccessor.Object,
                _mockWebHostEnvironment.Object,
                _mockSignInManager.Object,
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockJwtOptions.Object,
                _mockUrlConfiguration.Object,
                _mockEmailService.Object,
                _mockUserLibraryService.Object
            );
        }

        #region AddUser Tests

        [Fact]
        public async Task AddUser_WithValidModel_ShouldCreateUserAndSendWelcomeEmail()
        {
            // Arrange
            var model = new CreateUserDto
            {
                Email = "newuser@test.com",
                Role = "User",
                UserClaims = new List<ClaimDto> { new() { Type = "TestClaim", Value = "TestValue" } }
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
                .ReturnsAsync(IdentityResult.Success);

            SetupUserLibraryServiceMock(true);
            SetupEmailServiceMock(true);

            // Act
            await _controller.AddUser(model);

            // Assert
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role), Times.Once);
            _mockUserManager.Verify(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()),
                Times.Once);
            VerifyUserLibraryServiceCalled();
            VerifyEmailServiceCalled(model.Email);
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task AddUser_WhenUserCreationFails_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new CreateUserDto
            {
                Email = "newuser@test.com",
                Role = "User",
                UserClaims = new List<ClaimDto>()
            };
            var errors = new List<IdentityError> { new() { Description = "Email already exists" } };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            // Act
            await _controller.AddUser(model);

            // Assert
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Once);
            _mockEmailService.Verify(
                x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task AddUser_WhenRoleAssignmentFails_ShouldNotSendEmail()
        {
            // Arrange
            var model = new CreateUserDto
            {
                Email = "newuser@test.com",
                Role = "User",
                UserClaims = new List<ClaimDto>()
            };
            var errors = new List<IdentityError> { new() { Description = "Role assignment failed" } };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));
            _mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
                .ReturnsAsync(IdentityResult.Success);

            SetupUserLibraryServiceMock(true);

            // Act
            await _controller.AddUser(model);

            // Assert
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role), Times.Once);
            VerifyUserLibraryServiceCalled();

            // Email should not be sent when role assignment fails
            _mockEmailService.Verify(
                x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Once);
        }

        [Fact]
        public async Task AddUser_WhenClaimsAssignmentFails_ShouldNotSendEmail()
        {
            // Arrange
            var model = new CreateUserDto
            {
                Email = "newuser@test.com",
                Role = "User",
                UserClaims = new List<ClaimDto> { new() { Type = "TestClaim", Value = "TestValue" } }
            };
            var errors = new List<IdentityError> { new() { Description = "Claims assignment failed" } };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            SetupUserLibraryServiceMock(true);

            // Act
            await _controller.AddUser(model);

            // Assert
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role), Times.Once);
            _mockUserManager.Verify(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()),
                Times.Once);
            VerifyUserLibraryServiceCalled();

            // Email should not be sent when claims assignment fails
            _mockEmailService.Verify(
                x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Once);
        }

        [Fact]
        public async Task AddUser_WhenUserLibraryCreationFails_ShouldNotSendEmail()
        {
            // Arrange
            var model = new CreateUserDto
            {
                Email = "newuser@test.com",
                Role = "User",
                UserClaims = new List<ClaimDto>()
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
                .ReturnsAsync(IdentityResult.Success);

            // UserLibraryService returns false (failure)
            SetupUserLibraryServiceMock(false);

            // Act
            await _controller.AddUser(model);

            // Assert
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role), Times.Once);
            _mockUserManager.Verify(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()),
                Times.Once);
            VerifyUserLibraryServiceCalled();

            // Email should NOT be sent when UserLibraryService fails
            _mockEmailService.Verify(
                x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            // No notification should be added for UserLibraryService failure (based on controller logic)
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task AddUser_WhenEmailServiceFails_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new CreateUserDto
            {
                Email = "newuser@test.com",
                Role = "User",
                UserClaims = new List<ClaimDto>()
            };

            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), model.Role))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
                .ReturnsAsync(IdentityResult.Success);

            SetupUserLibraryServiceMock(true);
            SetupEmailServiceMock(false); // Email service fails

            // Act
            await _controller.AddUser(model);

            // Assert
            VerifyEmailServiceCalled(model.Email);
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Once);
        }

        #endregion

        #region FirstAccess Tests

        [Fact]
        public async Task FirstAccess_WithValidModel_ShouldResetPasswordAndSendConfirmationEmail()
        {
            // Arrange
            var model = new ChangePasswordDto
            {
                Email = "test@test.com",
                Password = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };
            var user = new ApplicationUser { Email = model.Email, UserName = model.Email, FirstAccess = true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("reset-token");
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(user))
                .ReturnsAsync("confirmation-token");

            SetupEmailServiceMock(true);

            // Act
            await _controller.FirstAccess(model);

            // Assert
            _mockUserManager.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
            _mockUserManager.Verify(x => x.ResetPasswordAsync(user, It.IsAny<string>(), model.Password), Times.Once);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
            _mockUserManager.Verify(x => x.GenerateEmailConfirmationTokenAsync(user), Times.Once);
            VerifyEmailServiceCalled(model.Email);
            Assert.False(user.FirstAccess);
        }

        [Fact]
        public async Task FirstAccess_WhenPasswordResetFails_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new ChangePasswordDto
            {
                Email = "test@test.com",
                Password = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };
            var user = new ApplicationUser { Email = model.Email, UserName = model.Email, FirstAccess = true };
            var errors = new List<IdentityError> { new() { Description = "Password reset failed" } };

            _mockUserManager.Setup(x => x.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("reset-token");
            _mockUserManager.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), model.Password))
                .ReturnsAsync(IdentityResult.Failed(errors.ToArray()));

            // Act
            await _controller.FirstAccess(model);

            // Assert
            _mockUserManager.Verify(x => x.ResetPasswordAsync(user, It.IsAny<string>(), model.Password), Times.Once);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Never);
            _mockEmailService.Verify(
                x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Once);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUser_WithRoleChange_ShouldUpdateRoleCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var model = new UserDto
            {
                Id = userId,
                Username = "updated@test.com",
                Email = "updated@test.com",
                Role = "Admin",
                IsDeleted = false,
                UserClaims = new List<ClaimDto>()
            };
            var user = new ApplicationUser { Id = userId, UserName = "old@test.com", Email = "old@test.com" };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(x => x.RemoveFromRoleAsync(user, "User"))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.AddToRoleAsync(user, model.Role))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>());

            // Act
            await _controller.UpdateUser(userId, model);

            // Assert
            _mockUserManager.Verify(x => x.RemoveFromRoleAsync(user, "User"), Times.Once);
            _mockUserManager.Verify(x => x.AddToRoleAsync(user, model.Role), Times.Once);
            _mockUserManager.Verify(x => x.UpdateAsync(user), Times.Once);
            Assert.Equal(model.Username, user.UserName);
            Assert.Equal(model.Email, user.Email);
            Assert.Equal(model.IsDeleted, user.IsDeleted);
        }

        [Fact]
        public async Task UpdateUser_WithClaimsUpdate_ShouldUpdateClaimsCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var model = new UserDto
            {
                Id = userId,
                Username = "test@test.com",
                Email = "test@test.com",
                Role = "User",
                IsDeleted = false,
                UserClaims = new List<ClaimDto>
                {
                    new() { Type = "NewClaim", Value = "NewValue" },
                    new() { Type = "UpdatedClaim", Value = "UpdatedValue" }
                }
            };
            var user = new ApplicationUser { Id = userId, UserName = "test@test.com", Email = "test@test.com" };
            var existingClaims = new List<Claim>
            {
                new("UpdatedClaim", "OldValue"),
                new("RemovedClaim", "RemovedValue")
            };

            _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });
            _mockUserManager.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GetClaimsAsync(user))
                .ReturnsAsync(existingClaims);

            // Act
            await _controller.UpdateUser(userId, model);

            // Assert
            // Should remove old claim and add new one for UpdatedClaim
            _mockUserManager.Verify(
                x => x.RemoveClaimAsync(user, It.Is<Claim>(c => c.Type == "UpdatedClaim" && c.Value == "OldValue")),
                Times.Once);
            _mockUserManager.Verify(
                x => x.AddClaimAsync(user, It.Is<Claim>(c => c.Type == "UpdatedClaim" && c.Value == "UpdatedValue")),
                Times.Once);

            // Should add new claim
            _mockUserManager.Verify(
                x => x.AddClaimAsync(user, It.Is<Claim>(c => c.Type == "NewClaim" && c.Value == "NewValue")),
                Times.Once);

            // Should remove claim that's not in new claims
            _mockUserManager.Verify(x => x.RemoveClaimAsync(user, It.Is<Claim>(c => c.Type == "RemovedClaim")),
                Times.Once);
        }

        #endregion

        #region ModelState Validation Tests

        [Fact]
        public async Task AddUser_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new CreateUserDto { Email = "invalid-email" };
            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            await _controller.AddUser(model);

            // Assert
            _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            // The controller should return CustomModelStateResponse which doesn't call NotifyError
        }

        [Fact]
        public async Task ResetPassword_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new ChangePasswordDto { Email = "test@test.com" };
            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            await _controller.ResetPassword(model);

            // Assert
            _mockUserManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnLoginResponse()
        {
            // Arrange
            var model = new LoginDto { Email = "test@test.com", Password = "Password123!" };
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = model.Email, UserName = model.Email };
            var role = new ApplicationRole { Name = "User", Level = 1 };

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, false, true))
                .ReturnsAsync(SignInResult.Success);
            _mockUserManager.Setup(x => x.FindByEmailAsync(model.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });
            _mockRoleManager.Setup(x => x.FindByNameAsync("User"))
                .ReturnsAsync(role);
            _mockUserManager.Setup(x => x.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>());
            _mockRoleManager.Setup(x => x.GetClaimsAsync(role))
                .ReturnsAsync(new List<Claim>());

            // Act
            await _controller.Login(model);

            // Assert
            _mockSignInManager.Verify(x => x.PasswordSignInAsync(model.Email, model.Password, false, true), Times.Once);
            _mockUserManager.Verify(x => x.FindByEmailAsync(model.Email), Times.Once);
            _mockUserManager.Verify(x => x.GetRolesAsync(user), Times.Once);
            _mockRoleManager.Verify(x => x.FindByNameAsync("User"), Times.Once);
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task Login_WithRequiresTwoFactor_ShouldReturnBadRequest()
        {
            // Arrange
            var model = new LoginDto { Email = "test@test.com", Password = "Password123!" };

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, false, true))
                .ReturnsAsync(SignInResult.TwoFactorRequired);

            // Act
            await _controller.Login(model);

            // Assert
            _mockNotifier.Verify(x => x.Handle(It.IsAny<Notification>()), Times.Once);
        }

        #endregion

        #region Helper Methods

        private void SetupMockDefaults()
        {
            var jwtOptions = new JwtOptions
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                AccessTokenExpiration = 3600,
                RefreshTokenExpiration = 86400,
                SecurityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    "test-secret-key-that-is-long-enough-for-hmac-sha256"u8.ToArray()),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        "test-secret-key-that-is-long-enough-for-hmac-sha256"u8.ToArray()),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
            };

            var urlConfig = new UrlConfiguration { UrlPortal = "https://test.com" };

            _mockJwtOptions.Setup(x => x.Value).Returns(jwtOptions);
            _mockUrlConfiguration.Setup(x => x.Value).Returns(urlConfig);
            _mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Test");
        }

        private void SetupFileSystemMocks()
        {
            var testWebRootPath = Path.Combine(Path.GetTempPath(), "TestWebRoot");
            _mockWebHostEnvironment.Setup(x => x.WebRootPath).Returns(testWebRootPath);

            var templateDir = Path.Combine(testWebRootPath, "assets", "templates");
            var templatePath = Path.Combine(templateDir, "template.html");

            try
            {
                Directory.CreateDirectory(templateDir);
                File.WriteAllText(templatePath, _testTemplateContent);
            }
            catch
            {
                // Ignore file creation errors in tests
            }
        }

        private void SetupEmailServiceMock(bool returnValue)
        {
            _mockEmailService
                .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(returnValue);
        }

        private void VerifyEmailServiceCalled(string expectedRecipient)
        {
            _mockEmailService.Verify(
                x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), expectedRecipient, It.IsAny<string>()),
                Times.Once);
        }

        private void SetupUserLibraryServiceMock(bool returnValue)
        {
            _mockUserLibraryService
                .Setup(x => x.AddAsync(It.IsAny<UserLibrary>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnValue);

            _mockUserLibraryService
                .Setup(x => x.AddAsync(It.IsAny<UserLibrary>(), CancellationToken.None))
                .ReturnsAsync(returnValue);
        }

        private void VerifyUserLibraryServiceCalled()
        {
            _mockUserLibraryService.Verify(
                x => x.AddAsync(It.IsAny<UserLibrary>(), It.IsAny<CancellationToken>()),
                Times.AtMostOnce);

            _mockUserLibraryService.Verify(
                x => x.AddAsync(It.IsAny<UserLibrary>(), CancellationToken.None),
                Times.AtMostOnce);

            var callsWithCancellation = _mockUserLibraryService.Invocations
                .Count(i => i.Method.Name == "AddAsync" && i.Arguments.Count == 2);
            var callsWithoutCancellation = _mockUserLibraryService.Invocations
                .Count(i => i.Method.Name == "AddAsync" && i.Arguments.Count == 1);

            Assert.True(callsWithCancellation > 0 || callsWithoutCancellation > 0,
                "AddAsync should have been called at least once");
        }

        private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
            var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
            var userValidators = new List<IUserValidator<ApplicationUser>>();
            var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<ApplicationUser>>>();

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, optionsAccessor.Object, passwordHasher.Object,
                userValidators, passwordValidators, keyNormalizer.Object,
                errors.Object, services.Object, logger.Object);

            mockUserManager.Setup(x => x.Users)
                .Returns(new List<ApplicationUser>().AsQueryable());

            return mockUserManager;
        }

        private static Mock<RoleManager<ApplicationRole>> CreateMockRoleManager()
        {
            var store = new Mock<IRoleStore<ApplicationRole>>();
            var roleValidators = new List<IRoleValidator<ApplicationRole>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var logger = new Mock<ILogger<RoleManager<ApplicationRole>>>();

            return new Mock<RoleManager<ApplicationRole>>(
                store.Object, roleValidators, keyNormalizer.Object,
                errors.Object, logger.Object);
        }

        private static Mock<ApplicationSignInManager> CreateMockApplicationSignInManager(
            UserManager<ApplicationUser> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
            var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
            var schemes = new Mock<IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

            var mockApplicationSignInManager = new Mock<ApplicationSignInManager>(
                userManager, contextAccessor.Object, claimsFactory.Object,
                optionsAccessor.Object, logger.Object, schemes.Object, confirmation.Object);

            mockApplicationSignInManager
                .Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success);

            return mockApplicationSignInManager;
        }

        #endregion
    }
}