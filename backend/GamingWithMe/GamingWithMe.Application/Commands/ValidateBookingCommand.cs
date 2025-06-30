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
        public Guid MentorId { get; set; }
        public string ClientId { get; set; }
        public BookingDetailsDto BookingDetailsDto { get; set; }

        public ValidateBookingCommand(BookingCommand originalCommand)
        {
            MentorId = originalCommand.mentorId;
            ClientId = originalCommand.clientId;
            BookingDetailsDto = originalCommand.BookingDetailsDto;
        }
    }
}
