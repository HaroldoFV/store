using System;
using CartMicroservice;
using CartMicroservice.Repository;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Middleware;
using MongoDB.Driver;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

builder.Host.UseSerilog((_, config) =>
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console());

// Add services to the container
var services = builder.Services;

services.AddControllers();

services.AddJwtAuthentication(builder.Configuration); // JWT Configuration

services.AddMongoDb(builder.Configuration);

services.AddSingleton<ICartRepository>(sp =>
    new CartRepository(sp.GetService<IMongoDatabase>() ?? throw new Exception("IMongoDatabase not found"))
);

services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {Title = "Catalog", Version = "v1"});

    c.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please insert JWT token with the prefix Bearer into field",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "Bearer"}
            },
            new string[] { }
        }
    });
});

services.AddHealthChecks()
    .AddMongoDb(
        mongodbConnectionString: (
            builder.Configuration.GetSection("mongo").Get<MongoOptions>()
            ?? throw new Exception("mongo configuration section not found")
        ).ConnectionString,
        name: "mongo",
        failureStatus: HealthStatus.Unhealthy
    );

services.AddHealthChecksUI().AddInMemoryStorage();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();

app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog V1"); });

var option = new RewriteOptions();
option.AddRedirect("^$", "swagger");
app.UseRewriter(option);

app.UseHealthChecks("/healthz",
    new HealthCheckOptions {Predicate = _ => true, ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse});

app.UseHealthChecksUI();

app.UseHttpsRedirection();

app.UseRouting();

// app.UseMiddleware<JwtMiddleware>(); // JWT Middleware

app.UseAuthorization();

app.MapControllers();

app.Run();