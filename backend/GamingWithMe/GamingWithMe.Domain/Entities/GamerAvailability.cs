using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class GamerAvailability
    {
        public Guid Id { get; set; }
        public Guid GamerId { get; set; }
        public Gamer Gamer { get; set; }

        public TimeSpan StartTime { get; set; } 
        public TimeSpan EndTime { get; set; }   
        public DayOfWeek DayOfWeek { get; set; }

        public GamerAvailability()
        {
            
        }

        public GamerAvailability(Guid gamerId, TimeSpan startTime, TimeSpan endTime, DayOfWeek dayOfWeek)
        {
            Id = Guid.NewGuid();
            GamerId = gamerId;
            StartTime = startTime;
            EndTime = endTime;
            DayOfWeek = dayOfWeek;
        }
    }

}
