using Amazon.S3;
using GamingWithMe.Api.Hubs;
using GamingWithMe.Application.DependencyInjection;
using GamingWithMe.Application.Handlers;
using GamingWithMe.Application.Interfaces;
using GamingWithMe.Application.Mappings;
using GamingWithMe.Domain.Entities;
using GamingWithMe.Infrastructure.Data;
using GamingWithMe.Infrastructure.DependencyInjection;
using GamingWithMe.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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


builder.Services.AddAWSService<IAmazonS3>(builder.Configuration.GetAWSOptions());
builder.Services.AddSingleton<IAmazonS3>(provider => {
    var config = provider.GetRequiredService<IConfiguration>();
    var accessKey = config["AWS:AccessKey"];
    var secretKey = config["AWS:SecretKey"];
    var region = config["AWS:Region"];

    return new AmazonS3Client(
        accessKey,
        secretKey,
        Amazon.RegionEndpoint.GetBySystemName(region ?? "eu-central-1")
    );
});


builder.Services.AddSignalR();

builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// AutoMapper – scan Profiles
builder.Services.AddAutoMapper(typeof(GameProfile).Assembly);
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(UserProfile).Assembly));

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

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
//})
//.AddCookie();
//.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
//{
//    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
//    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
//});




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

app.MapHub<ChatHub>("/chatHub");

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[] { "Admin", "Regular" };

    foreach (var item in roles)
    {
        if(!await roleManager.RoleExistsAsync(item))
        {
            await roleManager.CreateAsync(new IdentityRole(item));
        }
    }
}

app.Run();