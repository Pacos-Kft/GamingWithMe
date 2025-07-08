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
    public class DeleteLanguageFromGamerHandler : IRequestHandler<DeleteLanguageFromUserCommand, bool>
    {
        private readonly IAsyncRepository<User> _playerRepo;
        private readonly IAsyncRepository<Language> _languageRepo;

        public DeleteLanguageFromGamerHandler(
            IAsyncRepository<User> playerRepo,
            IAsyncRepository<Language> languageRepo)
        {
            _playerRepo = playerRepo;
            _languageRepo = languageRepo;
        }

        public async Task<bool> Handle(DeleteLanguageFromUserCommand request, CancellationToken cancellationToken)
        {
            var player = (await _playerRepo.ListAsync(cancellationToken)).FirstOrDefault(x => x.UserId == request.userId);

            if (player is null)
                throw new InvalidOperationException("Player not found");

            var language = (await _languageRepo.ListAsync(cancellationToken)).FirstOrDefault(x=> x.Name == request.language);
            if (language == null)
                throw new InvalidOperationException("Language not found.");

            var entry = player.Languages.FirstOrDefault(x => x.LanguageId == language.Id);

            if (entry != null) {
                player.Languages.Remove(entry);
                await _playerRepo.Update(player);
                return true;
            }

            return false;
        }
    }
}
