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
using System.Text.RegularExpressions;
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

            // 1. Check if email already exists
            var existingIdentityUser = await _userManager.FindByEmailAsync(dto.email);
            if (existingIdentityUser != null)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            // 2. Check if username already exists in your custom User table
            var existingCustomUser = (await _userRepo.ListAsync(cancellationToken)).FirstOrDefault(u => u.Username == dto.username);
            if (existingCustomUser != null)
            {
                throw new InvalidOperationException("This username is already taken. Please choose another one.");
            }

            var user = new IdentityUser
            {
                UserName = dto.email,
                Email = dto.email,
            };

            IdentityResult result;

            if (!string.IsNullOrWhiteSpace(dto.password))
            {
                // 3. Enforce password complexity for standard registration
                ValidatePassword(dto.password);
                result = await _userManager.CreateAsync(user, dto.password);
            }
            else
            {
                // This path is for external logins like Google, where no password is provided
                result = await _userManager.CreateAsync(user);
            }


            if (!result.Succeeded) {
                // Consolidate and throw specific error messages from Identity
                var errorMessages = result.Errors.Select(e => e.Description);
                throw new InvalidOperationException(string.Join("; ", errorMessages));
            }

            await _userRepo.AddAsync(new User(user.Id, dto.username));

            // Email confirmation logic can be re-enabled here if needed
            //var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            //// This URL is for local development and testing.
            //var confirmationLink = $"https://localhost:7091/api/account/confirm-email?userId={user.Id}&token={encodedToken}";

            //var emailVariables = new Dictionary<string, string>
            //{
            //    { "confirmation_link", confirmationLink }
            //};

            //// Use Template ID for registration confirmation
            //await _emailService.SendEmailAsync(dto.email, "Welcome to GamingWithMe!", 6953989, emailVariables);

            return user.Id;
            
        }

        private void ValidatePassword(string password)
        {
            if (password.Length < 6)
                throw new InvalidOperationException("Password must be at least 6 characters long.");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                throw new InvalidOperationException("Password must contain at least one uppercase letter.");
            if (!Regex.IsMatch(password, @"[a-z]"))
                throw new InvalidOperationException("Password must contain at least one lowercase letter.");
            if (!Regex.IsMatch(password, @"\d"))
                throw new InvalidOperationException("Password must contain at least one number.");
            if (!Regex.IsMatch(password, @"[\W_]")) // \W is any non-word character
                throw new InvalidOperationException("Password must contain at least one special character.");
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
