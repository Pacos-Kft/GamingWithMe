using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
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

        public GoogleLoginHandler(IAsyncRepository<User> userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<UserDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
        {
            //var user = (await _userRepo.ListAsync(cancellationToken))
            //    .FirstOrDefault(u => u.GoogleId == request.GoogleId || u.Email == request.Email);

            //if (user == null)
            //{
            //    user = new User
            //    {
            //        Id = Guid.NewGuid(),
            //        Username = request.Email,
            //        Email = request.Email,
            //        GoogleId = request.GoogleId,
            //        FullName = request.FullName,
            //        CreatedAt = DateTime.UtcNow
            //    };

            //    await _userRepo.AddAsync(user, cancellationToken);
            //}

            //return new UserDto
            //{
            //    Id = user.Id,
            //    Email = user.Email,
            //    FullName = user.FullName
            //};

            return null;
        }
    }

}
