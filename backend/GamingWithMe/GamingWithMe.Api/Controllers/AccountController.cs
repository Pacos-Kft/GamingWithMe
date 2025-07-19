using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(IMediator mediator, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IEmailService emailService)
        {
            _mediator = mediator;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;   
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
        {
            var id = await _mediator.Send(new RegisterProfileCommand(dto), cancellationToken);
            return Ok("Account created");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return NoContent();
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var command = new DeleteAccountCommand(userId);
                var result = await _mediator.Send(command);

                if (result)
                {
                    await _signInManager.SignOutAsync();
                    return Ok("Account deleted successfully.");
                }

                return BadRequest("Failed to delete account.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("login/google")]
        public IActionResult GoogleLogin([FromQuery] string returnUrl = "/")
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("login/google/callback")]
        public async Task<IActionResult> GoogleResponse([FromQuery] string returnUrl)
        {
            // Use the external scheme to get the authentication result
            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            
            if (!authenticateResult.Succeeded)
            {
                return BadRequest("Google authentication failed.");
            }

            var claims = authenticateResult.Principal.Claims;
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                return BadRequest("Required claims missing from Google authentication.");
            }

            try
            {
                var command = new GoogleLoginCommand
                {
                    GoogleId = googleId,
                    Email = email,
                    FullName = fullName ?? email
                };

                var userDto = await _mediator.Send(command);

                if (userDto == null)
                {
                    return BadRequest("Failed to process Google login.");
                }

                // Sign in the user with ASP.NET Identity
                var identityUser = await _userManager.FindByEmailAsync(email);
                if (identityUser != null)
                {
                    await _signInManager.SignInAsync(identityUser, isPersistent: false);
                }

                return Redirect("https://localhost:5173");
            }
            catch (Exception ex)
            {
                return BadRequest($"Google login failed: {ex.Message}");
            }
        }

        [HttpGet("login/facebook")]
        public IActionResult FacebookLogin([FromQuery] string returnUrl = "/")
        {
            var redirectUrl = Url.Action("FacebookResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet("login/facebook/callback")]
        public async Task<IActionResult> FacebookResponse([FromQuery] string returnUrl)
        {
            // Use the external scheme to get the authentication result
            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            
            if (!authenticateResult.Succeeded)
            {
                return BadRequest("Facebook authentication failed.");
            }

            var claims = authenticateResult.Principal.Claims;
            var facebookId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(facebookId) || string.IsNullOrEmpty(email))
            {
                return BadRequest("Required claims missing from Facebook authentication.");
            }

            try
            {
                var command = new FacebookLoginCommand
                {
                    FacebookId = facebookId,
                    Email = email,
                    FullName = fullName ?? email
                };

                var userDto = await _mediator.Send(command);

                if (userDto == null)
                {
                    return BadRequest("Failed to process Facebook login.");
                }

                // Sign in the user with ASP.NET Identity
                var identityUser = await _userManager.FindByEmailAsync(email);
                if (identityUser != null)
                {
                    await _signInManager.SignInAsync(identityUser, isPersistent: false);
                }

                return Redirect("https://localhost:5173");
            }
            catch (Exception ex)
            {
                return BadRequest($"Facebook login failed: {ex.Message}");
            }
        }
    }

    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
