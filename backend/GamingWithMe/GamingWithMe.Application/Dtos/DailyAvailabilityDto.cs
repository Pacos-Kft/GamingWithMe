using System;

namespace GamingWithMe.Application.Dtos
{
    public record DailyAvailabilityDto(
        DateTime Date,
        string StartTime,
        string EndTime,
        string SessionDuration,
        long price
    );
}