using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Domain.Entities
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public virtual User Sender { get; set; }
        public Guid ReceiverId { get; set; }
        public virtual User Receiver { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }

        public Message()
        {
            Id = Guid.NewGuid();
            SentAt = DateTime.UtcNow;
        }

        public Message(Guid senderId, Guid receiverId, string content) : this()
        {
            SenderId = senderId;
            ReceiverId = receiverId;
            Content = content;
        }
    }
}
