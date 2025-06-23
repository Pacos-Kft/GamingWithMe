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
    public class DeleteLanguageFromGamerHandler : IRequestHandler<DeleteLanguageFromGamerCommand, bool>
    {
        private readonly IAsyncRepository<Gamer> _playerRepo;
        private readonly IGamerReadRepository _esportRepo;
        private readonly IAsyncRepository<Language> _languageRepo;

        public DeleteLanguageFromGamerHandler(
            IAsyncRepository<Gamer> playerRepo,
            IAsyncRepository<Language> languageRepo,
            IGamerReadRepository esportRepo)
        {
            _playerRepo = playerRepo;
            _languageRepo = languageRepo;
            _esportRepo = esportRepo;
        }

        public async Task<bool> Handle(DeleteLanguageFromGamerCommand request, CancellationToken cancellationToken)
        {
            var player = await _esportRepo.GetByIdWithLanguagesAsync(request.userId, cancellationToken);

            if (player is not Gamer esportPlayer)
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
