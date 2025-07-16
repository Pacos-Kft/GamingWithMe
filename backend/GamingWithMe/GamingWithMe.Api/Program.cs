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
using GamingWithMe.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Stripe;
using Microsoft.Extensions.DependencyInjection;
using GamingWithMe.Api.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5173",
            "https://localhost:7091"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.Cookie.Name = "gamingwithme.auth";
    opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opt.Cookie.SameSite = SameSiteMode.None;
    opt.SlidingExpiration = true;
});

// Explicitly configure the external cookie
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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

builder.Services.Configure<MailjetSettings>(builder.Configuration.GetSection("MailjetSettings"));
builder.Services.AddScoped<IEmailService, MailjetEmailService>();


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

    options.OperationFilter<FileUploadOperationFilter>();
});

builder.Services.AddAuthentication()
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseCookiePolicy();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapIdentityApi<IdentityUser>()
    .RequireCors("AllowFrontend");
app.MapHub<ChatHub>("/chatHub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var configuration = services.GetRequiredService<IConfiguration>();
    var userRepo = services.GetRequiredService<IAsyncRepository<User>>();

    // Seed roles
    var roles = new[] { "Admin", "Regular" };
    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Seed Admin User
    var adminEmail = configuration["AdminUser:Email"];
    var adminUsername = configuration["AdminUser:Username"];
    var adminPassword = configuration["AdminUser:Password"];

    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword))
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser { UserName = adminUsername, Email = adminEmail, EmailConfirmed = true };
            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                // Create the corresponding custom User entity
                var customUser = new User(adminUser.Id, adminUsername);
                await userRepo.AddAsync(customUser);
            }
        }

        // Assign the Admin role if the user exists and is not already an admin
        if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

app.Run();