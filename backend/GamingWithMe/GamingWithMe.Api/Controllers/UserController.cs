using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;
using static System.Reflection.Metadata.BlobBuilder;

namespace GamingWithMe.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAsyncRepository<User> _userRepository;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(IMediator mediator, IAsyncRepository<User> userRepository, UserManager<IdentityUser> userManager)
        {
            _mediator = mediator;
            _userRepository = userRepository;
            _userManager = userManager;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = (await _userRepository.ListAsync()).FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Get the IdentityUser to check roles
            var identityUser = await _userManager.FindByIdAsync(userId);
            var isAdmin = identityUser != null && await _userManager.IsInRoleAsync(identityUser, "Admin");

            return Ok(new { id = user.Id, username = user.Username, isAdmin = isAdmin });
        }

        [HttpGet("billing-history")]
        public async Task<ActionResult<List<BillingRecordDto>>> GetBillingHistory()
        {
            var userId = GetUserId();
            var history = await _mediator.Send(new GetBillingHistoryQuery(userId));
            return Ok(history);
        }

        [HttpGet("upcoming-bookings")]
        public async Task<ActionResult<List<UpcomingBookingDto>>> GetUpcomingBookings()
        {
            var userId = GetUserId();
            var bookings = await _mediator.Send(new GetUpcomingBookingsQuery(userId));
            return Ok(bookings);
        }

        [HttpGet("profile/{username}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProfileDto>> GetProfile(string username)
        {
            var profile = await _mediator.Send(new GetUserProfileQuery(username));

            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpGet("profiles")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProfileDto>>> GetProfiles([FromQuery] string? tag = null)
        {
            var profiles = await _mediator.Send(new GetUserProfilesQuery(tag));

            return profiles == null ? NotFound() : Ok(profiles);
        }

        [HttpGet("top-by-bookings")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProfileDto>>> GetTopByBookings()
        {
            var profiles = await _mediator.Send(new GetTopUsersByBookingsQuery());
            return profiles == null ? NotFound() : Ok(profiles);
        }

        [HttpGet("random-with-stripe")]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProfileDto>>> GetRandomWithStripe()
        {
            var profiles = await _mediator.Send(new GetRandomUsersWithStripeQuery());
            return profiles == null ? NotFound() : Ok(profiles);
        }

        [HttpPut("bio")]
        public async Task<IActionResult> UpdateBio([FromBody] UpdateBioDto dto)
        {
            var userId = GetUserId();
            var result = await _mediator.Send(new UpdateUserBioCommand(userId, dto.Bio));
            return result ? Ok() : NotFound("User not found.");
        }

        [HttpPut("username")]
        public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return BadRequest("Username is required.");
            }

            try
            {
                var userId = GetUserId();
                var result = await _mediator.Send(new UpdateUsernameCommand(userId, dto.Username));
                return result ? Ok("Username updated successfully.") : BadRequest("Failed to update username.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest("Both current and new passwords are required.");
            }

            try
            {
                var userId = GetUserId();
                var result = await _mediator.Send(new ChangePasswordCommand(userId, dto.CurrentPassword, dto.NewPassword));
                return result ? Ok("Password changed successfully.") : BadRequest("Failed to change password.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatarFile)
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var userId = GetUserId();
            var newAvatarUrl = await _mediator.Send(new UpdateAvatarCommand(userId, avatarFile));

            return Ok(new { AvatarUrl = newAvatarUrl });
        }


        [HttpPost("languages")]
        public async Task<IActionResult> AddLanguage([FromBody] Guid languageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) { 
                return NotFound();
            }

            var added = await _mediator.Send(new AddLanguageToUserCommand(userId, languageId));

            return Ok(added);
        }

        [HttpDelete("languages")]
        public async Task<IActionResult> DeleteLanguage([FromBody] string language)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteLanguageFromUserCommand(userId, language));

            return Ok(removed);
        }

        [HttpPost("games")]
        public async Task<IActionResult> AddGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var added = await _mediator.Send(new AddGameToUserCommand(userId, gameId));

            return Ok(added);
        }

        [HttpDelete("games")]
        public async Task<IActionResult> DeleteGame([FromBody] Guid gameId)
        {
            var userId = GetUserId();

            var removed = await _mediator.Send(new DeleteGameFromUserCommand(userId, gameId));

            return Ok(removed);
        }

        

        [HttpPost("daily-availability")]
        public async Task<IActionResult> SetDailyAvailability([FromBody] DailyAvailabilityDto availability)
        {
            var userId = GetUserId();

            var result = await _mediator.Send(new SetDailyAvailabilityCommand(userId, availability));

            return result ? Ok(true) : BadRequest("Failed to set availability");
        }

        [HttpGet("daily-availability/{date}")]
        public async Task<IActionResult> GetDailyAvailability(DateTime date)
        {
            var userId = GetUserId();

            var availability = await _mediator.Send(new GetDailyAvailabilityQuery(userId, date));

            return Ok(availability);
        }

        [HttpDelete("daily-availability/{date}")]
        public async Task<IActionResult> DeleteDailyAvailability(DateTime date)
        {
            var userId = GetUserId();

            var result = await _mediator.Send(new DeleteDailyAvailabilityCommand(userId, date));

            return result ? Ok(true) : BadRequest("Failed to delete availability");
        }

        [HttpPost("tags")]
        public async Task<IActionResult> AddTag([FromBody] Guid tagId)
        {
            var userId = GetUserId();
            var added = await _mediator.Send(new AddUserTagCommand(userId, tagId));
            return added ? Ok(true) : BadRequest("Failed to add tag");
        }

        [HttpDelete("tags/{tagId}")]
        public async Task<IActionResult> RemoveTag(Guid tagId)
        {
            var userId = GetUserId();
            var removed = await _mediator.Send(new RemoveUserTagCommand(userId, tagId));
            return removed ? Ok(true) : BadRequest("Failed to remove tag");
        }


        private string GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                throw new Exception("User not found");
            }

            return userId;
        }
    }

    public class UpdateBioDto
    {
        public string Bio { get; set; }
    }

    public class UpdateUsernameDto
    {
        [Required]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters long.")]
        [MaxLength(50, ErrorMessage = "Username cannot be longer than 50 characters.")]
        public string Username { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; }
    }
}
