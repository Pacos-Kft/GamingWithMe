using GamingWithMe.Application.Dtos;
using MediatR;
using System.Collections.Generic;

namespace GamingWithMe.Application.Queries
{
    public record GetUpcomingBookingsQuery(string UserId) : IRequest<List<UpcomingBookingDto>>;
}