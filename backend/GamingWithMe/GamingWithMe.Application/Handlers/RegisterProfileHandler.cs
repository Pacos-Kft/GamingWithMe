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
    public class RegisterProfileHandler : IRequestHandler<RegisterProfileCommand, string>
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAsyncRepository<Gamer> _esportRepo;
        private readonly IAsyncRepository<User> _userRepo;

        public RegisterProfileHandler(UserManager<IdentityUser> userManager, IAsyncRepository<Gamer> esportRepo, IAsyncRepository<User> regularRepo)
        {
            _userManager = userManager;
            _esportRepo = esportRepo;
            _userRepo = regularRepo;
        }


        public async Task<string> Handle(RegisterProfileCommand request, CancellationToken cancellationToken)
        {
            var dto = request.RegisterDto;

            var user = new IdentityUser
            {
                UserName = dto.email,
                Email = dto.email,
            };

            var result = await _userManager.CreateAsync(user,dto.password);

            if (!result.Succeeded) {
                throw new InvalidOperationException(
                    string.Join(';', result.Errors));
            }

            var roleName = ToRole(dto.PlayerType);
            await _userManager.AddToRoleAsync(user, roleName);

            switch (dto.PlayerType)
            {
                case UserType.Gamer:
                   await _esportRepo.AddAsync(new Gamer(user.Id, dto.username));
                    break;
                case UserType.User:
                    await _userRepo.AddAsync(new User(user.Id, dto.username));
                    break;

            }
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
