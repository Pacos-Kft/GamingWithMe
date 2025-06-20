using GamingWithMe.Application.Interfaces;
using GamingWithMe.Infrastructure.Data;
using GamingWithMe.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingWithMe.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseSqlServer(config.GetConnectionString("Default"),
            x => x.MigrationsAssembly("GamingWithMe.Infrastructure")));

            services.AddIdentityCore<IdentityUser>()
                    .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));
            services.AddScoped<IGameRepository, GameRepository>();

            // Other infrastructure (e-mail, Stripe, etc.)

            return services;
        }
    }
}
