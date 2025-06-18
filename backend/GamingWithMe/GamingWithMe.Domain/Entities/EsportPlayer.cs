using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class EsportPlayer
    {
        public Guid Id { get; set; }

        public string UserId { get; set; }
        public virtual IdentityUser User { get; set; }

        public string AvatarUrl { get; set; }
        public string Bio {  get; set; }
        public ICollection<string> Languages { get; set; }
        //TODO Review
        public int Earnings { get; set; }
        public DateTime CreatedAt { get; set; }

        public EsportPlayer()
        {
            
        }

        public EsportPlayer(string userId) 
        {
            Id = Guid.NewGuid();
            UserId = userId;
            AvatarUrl = string.Empty;
            Bio = string.Empty;
            Languages = new List<string>();
            Earnings = 0;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
