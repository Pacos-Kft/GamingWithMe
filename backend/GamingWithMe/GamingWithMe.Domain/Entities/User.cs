using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }      

        public string UserId { get; set; }
        public virtual IdentityUser IdentityUser { get; set; }

        public string StripeAccount {  get; set; }
        public string StripeCustomer {  get; set; }

        public string Username { get; set; }
        public string Bio { get; set; }
        public bool IsActive { get; set; }


        public string GoogleId { get; set; }

        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Booking> Bookings { get; set; }
        public ICollection<Message> SentMessages { get; set; }
        public ICollection<Message> ReceivedMessages { get; set; }
        public ICollection<UserLanguage> Languages { get; set; }
        public ICollection<UserAvailability> DailyAvailability { get; set; }
        public ICollection<Product> Products { get; set; }
        public ICollection<UserGame> Games { get; set; }
        public ICollection<UserTag> Tags { get; set; }
        public ICollection<Discount> Discounts { get; set; }







        public User()
        {

        }

        public User(string userId, string username)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            Username = username;
            AvatarUrl = string.Empty;
            CreatedAt = DateTime.UtcNow;
            Bookings = new List<Booking>();
            StripeAccount = string.Empty;
            StripeCustomer = string.Empty;
            SentMessages = new List<Message>();
            ReceivedMessages = new List<Message>();
            GoogleId = string.Empty;
            Bio = string.Empty;
            Languages = new List<UserLanguage>();
            Games = new List<UserGame>();
            IsActive = false;
            DailyAvailability = new List<UserAvailability>();
            Products = new List<Product>();
            Tags = new List<UserTag>();
            Discounts = new List<Discount>();
        }
    }
}
