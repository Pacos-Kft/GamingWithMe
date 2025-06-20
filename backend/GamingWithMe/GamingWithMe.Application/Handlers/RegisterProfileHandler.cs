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
        private readonly IAsyncRepository<EsportPlayer> _esportRepo;
        private readonly IAsyncRepository<PlayerBase> _regularRepo;

        public RegisterProfileHandler(UserManager<IdentityUser> userManager, IAsyncRepository<EsportPlayer> esportRepo, IAsyncRepository<PlayerBase> regularRepo)
        {
            _userManager = userManager;
            _esportRepo = esportRepo;
            _regularRepo = regularRepo;
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

            //TODO assign roles

            switch (dto.PlayerType)
            {
                case PlayerType.Esport:
                   await _esportRepo.AddAsync(new EsportPlayer(user.Id, dto.username));
                    break;
                case PlayerType.Regular:
                    await _regularRepo.AddAsync(new RegularPlayer(user.Id, dto.username));
                    break;

            }
            return user.Id;
            
        }
    }
}
