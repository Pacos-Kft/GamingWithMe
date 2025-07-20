using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, UserDto>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMediator _mediator;

        public GoogleLoginHandler(IAsyncRepository<User> userRepo, UserManager<IdentityUser> userManager, IMediator mediator)
        {
            _userRepo = userRepo;
            _userManager = userManager;
            _mediator = mediator;
        }

        public async Task<UserDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            var identityUser = await _userManager.FindByEmailAsync(request.Email);
            
            if (identityUser != null)
            {
                var existingUser = (await _userRepo.ListAsync(cancellationToken))
                    .FirstOrDefault(u => u.UserId == identityUser.Id);

                if (existingUser != null)
                {
                    if (string.IsNullOrEmpty(existingUser.GoogleId) && !string.IsNullOrEmpty(request.GoogleId))
                    {
                        existingUser.GoogleId = request.GoogleId;
                        await _userRepo.Update(existingUser);
                    }

                    return new UserDto(identityUser.Email, existingUser.GoogleId, existingUser.FacebookId, request.FullName);
                }
            }

            var username = GenerateUsernameFromEmail(request.Email);
            
            var existingUsernames = (await _userRepo.ListAsync(cancellationToken))
                .Select(u => u.Username)
                .ToList();
            
            username = EnsureUniqueUsername(username, existingUsernames);

            var registerDto = new RegisterDto(
                email: request.Email,
                password: null, 
                username: username,
                googleId: request.GoogleId,
                facebookId: null
            );

            try
            {
                var userId = await _mediator.Send(new RegisterProfileCommand(registerDto), cancellationToken);
                
                var newUser = (await _userRepo.ListAsync(cancellationToken))
                    .FirstOrDefault(u => u.UserId == userId);
                
                if (newUser != null)
                {
                    newUser.GoogleId = request.GoogleId;
                    await _userRepo.Update(newUser);
                }

                return new UserDto(request.Email, request.GoogleId, null, request.FullName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create user account: {ex.Message}", ex);
            }
        }

        private string GenerateUsernameFromEmail(string email)
        {
            var username = email.Split('@')[0];
            username = System.Text.RegularExpressions.Regex.Replace(username, @"[^a-zA-Z0-9_]", "");
            return username;
        }

        private string EnsureUniqueUsername(string baseUsername, List<string> existingUsernames)
        {
            if (!existingUsernames.Contains(baseUsername))
            {
                return baseUsername;
            }

            int counter = 1;
            string uniqueUsername;
            do
            {
                uniqueUsername = $"{baseUsername}{counter}";
                counter++;
            }
            while (existingUsernames.Contains(uniqueUsername));

            return uniqueUsername;
        }
    }
}