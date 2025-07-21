using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Dtos
{
    public record BookingDto(Guid Id, string gamername, string username, DateTime startTime, TimeSpan duration, DateTime created);

    // New DTO for unified activity (both bookings and service orders)
    public record UserActivityDto(
        Guid Id,
        string Type, // "Booking" or "ServiceOrder"
        string Title,
        string OtherPartyUsername,
        string OtherPartyAvatarUrl,
        DateTime Date,
        string Status,
        long Price,
        TimeSpan? Duration = null, // Only for bookings
        DateTime? DeliveryDeadline = null // Only for service orders
    );
}
