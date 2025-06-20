using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class PlayerBase
    {
        public Guid Id { get; set; }      

        public string UserId { get; set; }
        public virtual IdentityUser User { get; set; }

        public string Username { get; set; }

        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        public PlayerBase()
        {

        }

        public PlayerBase(string userId, string username)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Username = username;
            AvatarUrl = string.Empty;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
