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
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, ProfileDto>()
                .ForMember(d => d.games,
                    opt => opt.MapFrom(src => src.Games.Select(g => g.Game.Name)))
                .ForMember(d => d.languages,
                    opt => opt.MapFrom(src => src.Languages.Select(l => l.Language.Name)))
                .ForMember(d => d.avatarurl, opt => opt.MapFrom(src => src.AvatarUrl))
                .ForMember(d => d.username, opt => opt.MapFrom(src => src.Username))
                .ForMember(d => d.bio, opt => opt.MapFrom(src => src.Bio))
                .ForMember(d => d.joined, opt => opt.MapFrom(src => src.CreatedAt));
        }
    }
}
