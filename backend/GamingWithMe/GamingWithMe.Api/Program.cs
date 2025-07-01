using GamingWithMe.Application.Mappings;
using GamingWithMe.Infrastructure.Data;
using GamingWithMe.Infrastructure.DependencyInjection;
using GamingWithMe.Application.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Infrastructure.Repositories;
using GamingWithMe.Application.Handlers;
using GamingWithMe.Domain.Entities;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.Cookie.Name = "gamingwithme.auth";
    opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opt.Cookie.SameSite = SameSiteMode.None;
    opt.SlidingExpiration = true;
});

builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGamerReadRepository, GamerReadRepository>();


builder.Services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
            x => x.MigrationsAssembly("GamingWithMe.Infrastructure")));


builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<BookingHandler>());


builder.Services.Configure<StripeModel>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ChargeService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccountLinkService>();
builder.Services.AddScoped<PriceService>();



// AutoMapper – scan Profiles
builder.Services.AddAutoMapper(typeof(GameProfile).Assembly);
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(GamerProfile).Assembly));

//builder.Services.AddInfrastructure(builder.Configuration);

//builder.Services.AddApplication();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "bearer"
    });

});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapIdentityApi<IdentityUser>()
    .RequireCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using(var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[] { "Admin", "Esport", "Regular" };

    foreach (var item in roles)
    {
        if(!await roleManager.RoleExistsAsync(item))
        {
            await roleManager.CreateAsync(new IdentityRole(item));
        }
    }
}

app.Run();