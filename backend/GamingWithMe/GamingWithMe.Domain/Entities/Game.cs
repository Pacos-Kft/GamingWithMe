using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class Game
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Slug { get; set; }

        public Game(string name, string description)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
            Slug = name.Replace(" ", "-");
        }
    }
}
