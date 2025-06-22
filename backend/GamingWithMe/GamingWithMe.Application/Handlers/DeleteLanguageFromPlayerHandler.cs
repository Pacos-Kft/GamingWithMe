using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class DeleteLanguageFromPlayerHandler : IRequestHandler<DeleteLanguageFromPlayerCommand, bool>
    {
        private readonly IAsyncRepository<EsportPlayer> _playerRepo;
        private readonly IEsportPlayerReadRepository _esportRepo;
        private readonly IAsyncRepository<Language> _languageRepo;

        public DeleteLanguageFromPlayerHandler(
            IAsyncRepository<EsportPlayer> playerRepo,
            IAsyncRepository<Language> languageRepo,
            IEsportPlayerReadRepository esportRepo)
        {
            _playerRepo = playerRepo;
            _languageRepo = languageRepo;
            _esportRepo = esportRepo;
        }

        public async Task<bool> Handle(DeleteLanguageFromPlayerCommand request, CancellationToken cancellationToken)
        {
            var player = await _esportRepo.GetByIdWithLanguagesAsync(request.userId, cancellationToken);

            if (player is not EsportPlayer esportPlayer)
                throw new InvalidOperationException("Player not found or not an Esport player.");

            var language = (await _languageRepo.ListAsync(cancellationToken)).FirstOrDefault(x=> x.Name == request.language);
            if (language == null)
                throw new InvalidOperationException("Language not found.");

            var entry = esportPlayer.Languages.FirstOrDefault(x => x.LanguageId == language.Id);

            if (entry != null) {
                player.Languages.Remove(entry);
                await _playerRepo.Update(player);
                return true;
            }

            return false;
        }
    }
}
