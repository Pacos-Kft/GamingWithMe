using GamingWithMe.Application.Commands;
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
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateGameCommand>());


            // AutoMapper – scan Profiles
            services.AddAutoMapper(typeof(GameProfile).Assembly);


            return services;
        }
    }
}
