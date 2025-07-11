using System;
using System.Collections.Generic;

namespace GamingWithMe.Application.Dtos
{
    public record ProfileDto(
        Guid Id,
        string username,
        string avatarurl,
        string bio,
        bool isActive,
        List<string> languages,
        List<string> games,
        List<string> tags,
        bool hasStripeAccount,        
        List<BookingSummaryDto> bookings, 
        List<AvailabilitySlotDto> availability, 
        DateTime joined
    );
    
    public record BookingSummaryDto(
        Guid Id,
        DateTime StartTime,
        TimeSpan Duration,
        string CustomerName
    );
}
