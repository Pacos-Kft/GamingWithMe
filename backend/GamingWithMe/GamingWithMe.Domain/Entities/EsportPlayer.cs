using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class EsportPlayer : PlayerBase
    {
        public string Bio {  get; set; }
        public ICollection<string> Languages { get; set; }
        //TODO Review
        public int Earnings { get; set; }

        public EsportPlayer() : base()
        {
            
        }

        public EsportPlayer(string userId, string username) : base(userId, username) 
        {
            Bio = string.Empty;
            Languages = new List<string>();
            Earnings = 0;
        }
    }
}
