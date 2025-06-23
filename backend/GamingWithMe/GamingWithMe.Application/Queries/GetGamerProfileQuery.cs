using GamingWithMe.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Queries
{
    public record GetGamerProfileQuery(string username) : IRequest<GamerDto?>;
}
