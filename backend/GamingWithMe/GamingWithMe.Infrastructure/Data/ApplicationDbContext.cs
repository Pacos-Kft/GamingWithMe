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
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Game> Games => Set<Game>();
        public new DbSet<User> Users => Set<User>();
        public DbSet<UserGame> UserGames => Set<UserGame>();
        public DbSet<Language> Languages => Set<Language>();
        public DbSet<UserLanguage> UserLanguages => Set<UserLanguage>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<UserAvailability> UserAvailabilities => Set<UserAvailability>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<GameNews> GameNews => Set<GameNews>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<UserTag> UserTags => Set<UserTag>();
        public DbSet<GameEvent> GameEvents => Set<GameEvent>();
        public DbSet<GameEasterEgg> GameEasterEggs => Set<GameEasterEgg>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Username).IsRequired();
                e.HasIndex(p => p.Username).IsUnique();

                e.HasOne(p => p.IdentityUser)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<GameEvent>(e =>
            {
                e.HasKey(x => x.Id);
            
                e.HasOne(x => x.Game)
                    .WithMany(g => g.Events)
                    .HasForeignKey(x => x.GameId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(x => x.PrizePool).HasPrecision(18, 2);
            });

            builder.Entity<GameEasterEgg>(e =>
            {
                e.HasKey(x => x.Id);

                e.HasOne(x => x.Game)
                    .WithMany(g => g.EasterEggs)
                    .HasForeignKey(x => x.GameId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserGame>(x =>
            {
                x.HasKey(eg => new { eg.PlayerId, eg.GameId });

                x.HasOne(eg => eg.Player)
                .WithMany(p => p.Games)
                .HasForeignKey(eg => eg.PlayerId);

                x.HasOne(eg => eg.Game)
                .WithMany(g => g.Players)
                .HasForeignKey(ep => ep.GameId);
            });

            builder.Entity<UserLanguage>(x =>
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

                b.HasOne(x => x.Provider)
                    .WithMany()
                    .HasForeignKey(x => x.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Customer)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(x => x.CustomerId)
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

            builder.Entity<UserTag>(x =>
            {
                x.HasKey(ut => new { ut.UserId, ut.TagId });

                x.HasOne(ut => ut.User)
                    .WithMany(u => u.Tags)
                    .HasForeignKey(ut => ut.UserId);

                x.HasOne(ut => ut.Tag)
                    .WithMany(t => t.Users)
                    .HasForeignKey(ut => ut.TagId);
            });

            builder.Entity<Tag>().HasData(
                new Tag("Gamer") { Id = new Guid("8f676a1d-3d9a-42e7-9476-9a8d88340f53") },
                new Tag("Tiktoker") { Id = new Guid("056f4d3c-6fed-4892-82dc-2f5939f2cdc1") },
                new Tag("Youtuber") { Id = new Guid("2a764c79-6825-4cfa-ad69-3121c33ea657") },
                new Tag("Musician") { Id = new Guid("d7c3984d-5ef3-4a3e-af8d-ff76a38ce679") },
                new Tag("Just chatting") { Id = new Guid("097de202-a9e8-40cf-9648-5ab402fa802d") }
            );
        }
    }
}
