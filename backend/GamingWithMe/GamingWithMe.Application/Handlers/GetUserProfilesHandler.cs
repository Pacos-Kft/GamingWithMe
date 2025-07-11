﻿using AutoMapper;
using GamingWithMe.Application.Dtos;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Queries;
using GamingWithMe.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamingWithMe.Application.Handlers
{
    public class GetUserProfilesHandler : IRequestHandler<GetUserProfilesQuery, List<ProfileDto>>
    {
        private readonly IAsyncRepository<User> _userRepo;
        private readonly IAsyncRepository<Language> _languageRepo;
        private readonly IAsyncRepository<Game> _gameRepo;
        private readonly IAsyncRepository<Tag> _tagRepo;
        private readonly IMapper _mapper;

        public GetUserProfilesHandler(
            IAsyncRepository<User> userRepo,
            IAsyncRepository<Language> languageRepo,
            IAsyncRepository<Game> gameRepo,
            IAsyncRepository<Tag> tagRepo,
            IMapper mapper)
        {
            _userRepo = userRepo;
            _languageRepo = languageRepo;
            _gameRepo = gameRepo;
            _tagRepo = tagRepo;
            _mapper = mapper;
        }

        public async Task<List<ProfileDto>> Handle(GetUserProfilesQuery request, CancellationToken cancellationToken)
        {
            // Get users with all their relationships
            var users = await _userRepo.ListAsync(cancellationToken, 
                x => x.Languages, 
                x => x.Games, 
                x => x.Tags,
                x => x.DailyAvailability, 
                x => x.Products,
                x => x.Bookings);

            // Pre-load all related entities for efficient lookups
            var allLanguages = await _languageRepo.ListAsync(cancellationToken);
            var allGames = await _gameRepo.ListAsync(cancellationToken);
            var allTags = await _tagRepo.ListAsync(cancellationToken);

            // Create dictionaries for fast lookups
            var languagesDict = allLanguages.ToDictionary(l => l.Id);
            var gamesDict = allGames.ToDictionary(g => g.Id);
            var tagsDict = allTags.ToDictionary(t => t.Id);

            // Map user entities to profile DTOs with proper relationships
            var profiles = new List<ProfileDto>();
            
            foreach (var user in users)
            {
                // Link related entities
                foreach (var ul in user.Languages)
                {
                    if (languagesDict.TryGetValue(ul.LanguageId, out var lang))
                        ul.Language = lang;
                }
                
                foreach (var ug in user.Games)
                {
                    if (gamesDict.TryGetValue(ug.GameId, out var game))
                        ug.Game = game;
                }
                
                foreach (var ut in user.Tags)
                {
                    if (tagsDict.TryGetValue(ut.TagId, out var tag))
                        ut.Tag = tag;
                }
                
                // Map the user to a profile DTO with all the linked data
                var profile = _mapper.Map<ProfileDto>(user);
                profiles.Add(profile);
            }

            return profiles;
        }
    }
}
