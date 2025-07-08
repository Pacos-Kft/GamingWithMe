using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class DeleteGameEasterEggHandler : IRequestHandler<DeleteGameEasterEggCommand, bool>
    {
        private readonly IAsyncRepository<GameEasterEgg> _easterEggRepository;

        public DeleteGameEasterEggHandler(IAsyncRepository<GameEasterEgg> easterEggRepository)
        {
            _easterEggRepository = easterEggRepository;
        }

        public async Task<bool> Handle(DeleteGameEasterEggCommand request, CancellationToken cancellationToken)
        {
            var easterEgg = await _easterEggRepository.GetByIdAsync(request.EasterEggId, cancellationToken);
            if (easterEgg == null)
            {
                return false;
            }

            await _easterEggRepository.Delete(easterEgg);
            return true;
        }
    }
}