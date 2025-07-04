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
    public class MessageProfile : Profile
    {
        public MessageProfile()
        {
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderName, opt => opt.Ignore())
                .ForMember(dest => dest.ReceiverName, opt => opt.Ignore());
        }
    }
}
