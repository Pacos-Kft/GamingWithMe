using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Dtos
{
    public record BookingDto(Guid Id, string gamername, string username, DateTime startTime, TimeSpan duration, DateTime created);
    
}
