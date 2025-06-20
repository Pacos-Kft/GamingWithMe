using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class EsportPlayerLanguage
    {
        public Guid PlayerId { get; set; }
        public virtual EsportPlayer Player { get; set; }

        public Guid LanguageId { get; set; }
        public virtual Language Language { get; set; }


    }
}
