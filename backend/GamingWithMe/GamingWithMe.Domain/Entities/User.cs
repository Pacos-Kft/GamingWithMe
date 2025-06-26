using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }      

        public string UserId { get; set; }
        public virtual IdentityUser IdentityUser { get; set; }

        public string Username { get; set; }

        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Booking> Bookings { get; set; }



        public User()
        {

        }

        public User(string userId, string username)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Username = username;
            AvatarUrl = string.Empty;
            CreatedAt = DateTime.UtcNow;
            Bookings = new List<Booking>();

        }
    }
}
