using GamingWithMe.Domain.Entities;
using Microsoft.AspNetCore.Identity;
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
        public DbSet<Gamer> EsportPlayers => Set<Gamer>();
        public DbSet<RegularPlayer> RegularPlayers => Set<RegularPlayer>();
        public DbSet<GamerGame> EsportGames => Set<GamerGame>();
        public DbSet<Language> Languages => Set<Language>();
        public DbSet<GamerLanguage> EsportPlayerLanguages => Set<GamerLanguage>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(e =>
            {
                e.ToTable("Players");

                e.HasKey(p => p.Id);
                e.Property(p => p.Username).IsRequired();
                e.HasIndex(p => p.Username).IsUnique();

                e.HasOne(p => p.User)                  
                 .WithMany()                           
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);  
            });

            builder.Entity<Gamer>().ToTable("EsportPlayers");
            builder.Entity<RegularPlayer>().ToTable("RegularPlayers");

            builder.Entity<GamerGame>(x =>
            {
                x.HasKey(eg => new { eg.PlayerId, eg.GameId });

                x.HasOne(eg => eg.Player)
                .WithMany(p => p.Games)
                .HasForeignKey(eg => eg.PlayerId);

                x.HasOne(eg => eg.Game)
                .WithMany(g => g.EsportPlayers)
                .HasForeignKey(ep => ep.GameId);
            });

            builder.Entity<GamerLanguage>(x =>
            {
                x.HasKey(epl => new { epl.PlayerId, epl.LanguageId });

                x.HasOne(epl => epl.Player)
                .WithMany(p => p.Languages)
                .HasForeignKey(epl => epl.PlayerId);

                x.HasOne(epl => epl.Language)
                .WithMany(g => g.Players)
                .HasForeignKey(epl => epl.LanguageId);
            });



        }
    }
}
