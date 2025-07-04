using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class GetBookingByIdHandler : IRequestHandler<GetBookingByIdQuery, Booking?>
    {
        private readonly IAsyncRepository<Booking> _bookRepo;

        public GetBookingByIdHandler(IAsyncRepository<Booking> bookRepo)
        {
            _bookRepo = bookRepo;
        }

        public async Task<Booking?> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var booking = await _bookRepo.GetByIdAsync(request.Id, cancellationToken, b => b.Gamer, b => b.User);

            if (booking == null)
            {
                throw new InvalidOperationException("Booking not found");
            }

            //if (booking.Gamer == null || booking.User == null)
            //{
            //    throw new InvalidOperationException("Gamer or user not found");
            //}

            //return new BookingDto(
            //    booking.Id,
            //    booking.Gamer.Username,
            //    booking.User.Username,
            //    booking.StartTime,
            //    booking.Duration,
            //    booking.CreatedAt
            //);

            return booking;
        }

        
    }
}
