using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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

        public UserController(IMediator mediator, IAsyncRepository<User> userRepository)
        {
            _mediator = mediator;
            _userRepository = userRepository;
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

            return Ok(new { id = user.Id, username = user.Username });
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

        [HttpPut("status/active")]
        public async Task<ActionResult<bool>> SetIsActive()
        {
            var userId = GetUserId();
            
            var activity = await _mediator.Send(new SetUserActivityCommand(userId));

            return Ok(activity);
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
}
