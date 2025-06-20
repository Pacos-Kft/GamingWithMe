using GamingWithMe.Application.Commands;
using GamingWithMe.Application.Handlers;
using GamingWithMe.Application.Mappings;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Application.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // MediatR – scan Application assembly
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGameHandler>());
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetAllGamesHandler>());
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetGameByIdHandler>());


            // AutoMapper – scan Profiles
            services.AddAutoMapper(typeof(GameProfile).Assembly);


            return services;
        }
    }
}
