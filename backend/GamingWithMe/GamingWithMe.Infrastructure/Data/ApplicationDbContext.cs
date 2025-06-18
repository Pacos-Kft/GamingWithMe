using GamingWithMe.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext (DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<EsportPlayer> EsportPlayers => Set<EsportPlayer>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<EsportPlayer>(e =>
            {
                e.HasKey(p => p.Id);
                e.HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });


        }
    }
}
