using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class Gamer : User
    {
        public string Bio {  get; set; }
        public ICollection<GamerLanguage> Languages { get; set; }
        public int Earnings { get; set; }
        public ICollection<GamerGame> Games { get; set; }

        public bool IsActive { get; set; }
        public ICollection<GamerAvailability> WeeklyAvailability { get; set; }

        //TODO Review


        public Gamer() : base()
        {
            
        }

        public Gamer(string userId, string username) : base(userId, username) 
        {
            Bio = string.Empty;
            Languages = new List<GamerLanguage>();
            Earnings = 0;
            Games = new List<GamerGame>();
            IsActive = false;
            WeeklyAvailability = new List<GamerAvailability>();
        }
    }
}
