using AutoMapper;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Mappings
{
    public sealed class GameProfile : Profile
    {
        public GameProfile() {
            CreateMap<GameDto, Game>().ForMember(x => x.Slug, opt => opt.Ignore());

            CreateMap<GameDto, Game>();
        }
    }
}
