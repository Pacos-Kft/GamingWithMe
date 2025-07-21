using GamingWithMe.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;

namespace GamingWithMe.Application.Queries
{
    public record GetFixedServicesQuery(
        string? UserId = null,
        string? Category = null,
        bool? IsCustomService = null
    ) : IRequest<List<FixedServiceDto>>;
}