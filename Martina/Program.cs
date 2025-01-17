﻿using System.Text;
using Martina.Extensions;
using Martina.Models;
using Martina.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration.GetConnectionString("MongoDB");
if (connectionString is null)
{
    throw new InvalidOperationException("Failed to MongoDB connection string.");
}

builder.Services.AddControllers();
builder.Services.AddMartinaSwagger();
builder.Services.AddAuthorization(options => options.AddHotelRoleRequirement());
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
    options =>
    {
        JsonWebTokenOption? jsonWebTokenOption = builder.Configuration.GetSection(JsonWebTokenOption.OptionName)
            .Get<JsonWebTokenOption>();

        if (jsonWebTokenOption is null)
        {
            throw new InvalidOperationException("Failed to get JWT options");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jsonWebTokenOption.Issuer,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jsonWebTokenOption.JsonWebTokenKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });
builder.Services.AddDbContext<MartinaDbContext>(options =>
{
    options.UseMongoDB(connectionString, "Martina");
});
builder.Services.Configure<JsonWebTokenOption>(
    builder.Configuration.GetSection(JsonWebTokenOption.OptionName));
builder.Services.Configure<SystemUserOption>(
    builder.Configuration.GetSection(SystemUserOption.OptionName));
builder.Services.Configure<TimeOption>(
    builder.Configuration.GetSection(TimeOption.OptionName));
builder.Services.AddSingleton<SecretsService>();
builder.Services.AddSingleton<LifetimeService>();
builder.Services.AddHostedService<LifetimeService>(provider => provider.GetRequiredService<LifetimeService>());
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CheckinService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddSingleton<AirConditionerManageService>();
builder.Services.AddBuptSchedular();
builder.Services.AddScoped<IAuthorizationHandler, HotelRoleHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CheckinHandler>();
builder.Services.AddHostedService<TimeService>();
builder.Services.AddScoped<BillService>();
builder.Services.AddScoped<AirConditionerTestService>();
builder.Services.AddScoped<ManagerService>();

WebApplication application = builder.Build();
builder.WebHost.UseUrls("http://0.0.0.0:8080");


if (application.Environment.IsDevelopment())
{
    application.UseSwagger();
    application.UseSwaggerUI();
}

application.UseWebSockets();

application.UseAuthentication();
application.UseAuthorization();

application.UseStaticFiles();

application.MapControllers();
application.MapFallbackToFile("/index.html");

await application.RunAsync();
