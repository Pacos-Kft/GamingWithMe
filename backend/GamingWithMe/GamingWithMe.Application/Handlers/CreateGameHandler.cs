using GamingWithMe.Application.Commands;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    internal class CreateGameHandler : IRequestHandler<CreateGameCommand, Guid>
    {
        public Task<Guid> Handle(CreateGameCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
