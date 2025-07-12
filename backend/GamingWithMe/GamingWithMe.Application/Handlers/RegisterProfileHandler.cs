using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class RegisterProfileHandler : IRequestHandler<RegisterProfileCommand, string>
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IEmailService _emailService;

        public RegisterProfileHandler(UserManager<IdentityUser> userManager, IAsyncRepository<User> regularRepo, IEmailService emailService)
        {
            _userManager = userManager;
            _userRepo = regularRepo;
            _emailService = emailService;
        }


        public async Task<string> Handle(RegisterProfileCommand request, CancellationToken cancellationToken)
        {
            var dto = request.RegisterDto;

            var user = new IdentityUser
            {
                UserName = dto.email,
                Email = dto.email,
            };

            IdentityResult result;

            if (!string.IsNullOrWhiteSpace(dto.password))
            {
                result = await _userManager.CreateAsync(user, dto.password);
            }
            else
            {
                result = await _userManager.CreateAsync(user);
            }


            if (!result.Succeeded) {
                throw new InvalidOperationException(
                    string.Join(';', result.Errors));
            }

            //var roleName = ToRole(dto.PlayerType);
            //await _userManager.AddToRoleAsync(user, roleName);

            //switch (dto.PlayerType)
            //{
            //    case UserType.Gamer:
            //       await _esportRepo.AddAsync(new Gamer(user.Id, dto.username));
            //        break;
            //    case UserType.User:
            //        await _userRepo.AddAsync(new User(user.Id, dto.username));
            //        break;

            //}




            await _userRepo.AddAsync(new User(user.Id, dto.username));

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // This URL is for local development and testing.
            var confirmationLink = $"https://localhost:7091/api/account/confirm-email?userId={user.Id}&token={encodedToken}";

            var emailVariables = new Dictionary<string, string>
            {
                { "confirmation_link", confirmationLink }
            };

            // Use Template ID for registration confirmation
            await _emailService.SendEmailAsync(dto.email, "Welcome to GamingWithMe!", 6953989, emailVariables);
            return user.Id;
            
        }

        private static string ToRole(UserType type) =>
            type switch
        {
            UserType.Gamer   => "Esport",
            UserType.User  => "Regular",
            _                   => throw new ArgumentOutOfRangeException(nameof(type))
         };

    }
}
