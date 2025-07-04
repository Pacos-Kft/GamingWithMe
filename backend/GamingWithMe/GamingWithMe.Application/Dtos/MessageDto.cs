using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Dtos
{
    public record MessageDto(
        Guid Id,
        Guid SenderId,
        string SenderName,
        Guid ReceiverId,
        string ReceiverName,
        string Content,
        DateTime SentAt);
}
