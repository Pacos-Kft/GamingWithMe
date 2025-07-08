using GamingWithMe.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Commands
{
    public class ValidateBookingCommand : IRequest<bool>
    {
        public Guid ProviderId { get; set; }
        public string CustomerId { get; set; }
        public BookingDetailsDto BookingDetailsDto { get; set; }

        public ValidateBookingCommand(BookingCommand originalCommand)
        {
            ProviderId = originalCommand.providerId;
            CustomerId = originalCommand.customerId;
            BookingDetailsDto = originalCommand.BookingDetailsDto;
        }
    }
}
