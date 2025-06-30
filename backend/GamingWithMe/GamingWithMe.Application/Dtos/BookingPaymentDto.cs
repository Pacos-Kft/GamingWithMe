using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Dtos
{
    public record BookingPaymentDto
    (
         Guid GamerId,
         Guid UserId,
         string From,
         string Duration,
         string ConnectedAccountId,
         string priceId
    );

}
