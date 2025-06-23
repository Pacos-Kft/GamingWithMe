using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Commands
{
    public record AddLanguageToGamerCommand(string userId, Guid LanguageId) : IRequest<Guid>;
}
