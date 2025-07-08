using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class UserAvailability
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        public TimeSpan StartTime { get; set; } 
        public TimeSpan EndTime { get; set; }   
        public DayOfWeek DayOfWeek { get; set; }

        public UserAvailability()
        {
            
        }

        public UserAvailability(Guid gamerId, TimeSpan startTime, TimeSpan endTime, DayOfWeek dayOfWeek)
        {
            Id = Guid.NewGuid();
            UserId = gamerId;
            StartTime = startTime;
            EndTime = endTime;
            DayOfWeek = dayOfWeek;
        }
    }

}
