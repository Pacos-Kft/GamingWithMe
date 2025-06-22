using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class EsportGame
    {
        public Guid PlayerId { get; set; }
        public virtual EsportPlayer Player { get; set; }

        public Guid GameId { get; set; }
        public virtual Game Game { get; set; }

        public EsportGame()
        {
            
        }
    }
}
