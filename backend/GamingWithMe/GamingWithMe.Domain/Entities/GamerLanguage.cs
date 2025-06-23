using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class GamerLanguage
    {
        public Guid PlayerId { get; set; }
        public virtual Gamer Player { get; set; }

        public Guid LanguageId { get; set; }
        public virtual Language Language { get; set; }


    }
}
