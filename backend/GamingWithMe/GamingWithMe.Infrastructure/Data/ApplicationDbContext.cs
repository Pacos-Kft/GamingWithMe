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
        public DbSet<Gamer> Gamers => Set<Gamer>();
        //public DbSet<User> Users => Set<User>();
        public DbSet<GamerGame> GamerGames => Set<GamerGame>();
        public DbSet<Language> Languages => Set<Language>();
        public DbSet<GamerLanguage> GamerLanguages => Set<GamerLanguage>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<GamerAvailability> GamerAvailabilities => Set<GamerAvailability>();
        public DbSet<Product> Products => Set<Product>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(e =>
            {
                e.ToTable("Users");

                e.HasKey(p => p.Id);
                e.Property(p => p.Username).IsRequired();
                e.HasIndex(p => p.Username).IsUnique();

                e.HasOne(p => p.IdentityUser)                  
                 .WithMany()                           
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);  
            });

            builder.Entity<Gamer>().ToTable("Gamers");
            //builder.Entity<User>().ToTable("Users");

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

            builder.Entity<Booking>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasOne(x => x.Gamer)
                    .WithMany()
                    .HasForeignKey(x => x.GamerId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.User)
                    .WithMany(u => u.Bookings) 
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Message>(m =>
            {
                m.HasKey(x => x.Id);

                m.HasOne(x => x.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(x => x.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                m.HasOne(x => x.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(x => x.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });



        }
    }
}
