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
    public class AddLanguageToGamerHandler : IRequestHandler<AddLanguageToGamerCommand, Guid>
    {
        private readonly IAsyncRepository<Gamer> _playerRepo;
        private readonly IGamerReadRepository _esportRepo;
        private readonly IAsyncRepository<Language> _languageRepo;

        public AddLanguageToGamerHandler(
            IAsyncRepository<Gamer> playerRepo,
            IAsyncRepository<Language> languageRepo,
            IGamerReadRepository esportRepo)
        {
            _playerRepo = playerRepo;
            _languageRepo = languageRepo;
            _esportRepo = esportRepo;
        }


        public async Task<Guid> Handle(AddLanguageToGamerCommand request, CancellationToken cancellationToken)
        {
            var player = await _esportRepo.GetByIdWithLanguagesAsync(request.userId, cancellationToken);

            if (player is not Gamer esportPlayer)
                throw new InvalidOperationException("Player not found or not an Esport player.");

            var language = await _languageRepo.GetByIdAsync(request.LanguageId, cancellationToken);
            if (language == null)
                throw new InvalidOperationException("Language not found.");

            var alreadyHasLanguage = esportPlayer.Languages.Any(x=> x.LanguageId == language.Id);

            if (!alreadyHasLanguage) {
                esportPlayer.Languages.Add(new GamerLanguage
                {
                    LanguageId = request.LanguageId,
                    PlayerId = esportPlayer.Id
                });
            }

            await _playerRepo.Update(esportPlayer);

            return request.LanguageId;
        }
    }
}
