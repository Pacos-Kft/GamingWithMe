using AutoMapper;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Domain.Entities;

namespace GamingWithMe.Application.Mappings
{
    public class FixedServiceProfile : Profile
    {
        public FixedServiceProfile()
        {
            // Map from FixedService entity to FixedServiceDto
            CreateMap<FixedService, FixedServiceDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(d => d.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(d => d.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(d => d.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(d => d.DeliveryDeadline, opt => opt.MapFrom(src => src.DeliveryDeadline))
                .ForMember(d => d.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(d => d.Username, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(d => d.AvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl))
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

            // Map from CreateFixedServiceDto to FixedService entity
            CreateMap<CreateFixedServiceDto, FixedService>()
                .ForMember(d => d.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(d => d.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(d => d.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(d => d.DeliveryDeadline, opt => opt.MapFrom(src => src.DeliveryDeadline))
                .ForMember(d => d.Id, opt => opt.Ignore()) // Generated in constructor
                .ForMember(d => d.Status, opt => opt.Ignore()) // Set to default Active
                .ForMember(d => d.UserId, opt => opt.Ignore()) // Set separately
                .ForMember(d => d.User, opt => opt.Ignore()) // Navigation property
                .ForMember(d => d.CreatedAt, opt => opt.Ignore()); // Set in constructor
        }
    }
}