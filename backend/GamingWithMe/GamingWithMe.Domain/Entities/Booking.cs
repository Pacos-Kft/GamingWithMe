using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid GamerId { get; set; }
        public Gamer Gamer { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }

        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CreatedAt { get; set; }

        public Booking()
        {
            
        }

        public Booking(Guid GamerId, Guid UserId, DateTime StartTime, TimeSpan Duration)
        {
            this.GamerId = GamerId;
            this.UserId = UserId;
            this.StartTime = StartTime;
            this.Duration = Duration;
            CreatedAt = DateTime.UtcNow;
        }

    }
}
