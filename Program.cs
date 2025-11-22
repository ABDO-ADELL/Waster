using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using Scalar.AspNetCore;
using System.Configuration;
using System.Text;
using System.Text.Json.Serialization; // Add this
using Waster.Controllers;
using Waster.Helpers;
using Waster.Models;
using Waster.Services;


namespace Waster
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container with JSON configuration
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Handle circular references
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    // Increase max depth to handle deeply nested objects
                    options.JsonSerializerOptions.MaxDepth = 128;
                    // Optional: write indented for readability
                    options.JsonSerializerOptions.WriteIndented = false;
                });

            builder.Services.Configure<Jwt>(builder.Configuration.GetSection("Jwt"));

            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
                options.Lockout.MaxFailedAccessAttempts = 6;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;


            }).AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();


            builder.Services.AddScoped<IFileStorageService, FileStorageService>();

            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection")
                ));


            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddTransient(typeof(IBaseReporesitory<>), typeof(BaseReporesitory<>));
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.SaveToken = false;
                o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),

                };
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddScoped<IBookMarkRepository, BookMarkRepository>();
            builder.Services.AddScoped<IBrowseRepository, BrowseRepository>();

            // Update Unit of Work registration (if you already have it, just keep one)
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


            // Configure JSON options globally for OpenAPI
            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.SerializerOptions.MaxDepth = 256; // Increase significantly
            });

            // Add OpenAPI with type exclusions
            builder.Services.AddOpenApi(options =>
            {
                options.AddSchemaTransformer((schema, context, cancellationToken) =>
                {
                    // Exclude problematic Identity types from schema generation
                    if (context.JsonTypeInfo.Type == typeof(Microsoft.AspNetCore.Identity.IdentityUser) ||
                        context.JsonTypeInfo.Type == typeof(AppUser))
                    {
                        return Task.CompletedTask;
                    }
                    return Task.CompletedTask;
                });
            });

            //***************




            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
                    )
                };
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                googleOptions.SaveTokens = true;
            });



            //************************
            // Add CORS to allow browser access
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            builder.Services.AddSwaggerGen();

            var app = builder.Build();


            //Client ID  1018853528316-972ch9prapdv8e6p5g6jrcepa71k8hod.apps.googleusercontent.com
            // Client secret  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
            //UserSecretsId to 'fa560e95-423e-4cb5-8e02-0a08c75dd3ee'

            /*When deploying the app, either:

    Update the app's redirect URI in the Google Console to the app's deployed redirect URI.
    Create a new Google API registration in the Google Console for the production app with its production redirect URI.
**/

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    // Enable CORS for images
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
                }
            });
            app.UseCors("AllowFrontend");

            // Enable CORS
            app.UseCors("AllowAll");

            // Map OpenAPI endpoint
            app.MapOpenApi();

            // Map Scalar with custom configuration
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Waster API Documentation")
                    .WithTheme(ScalarTheme.Mars)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });

            // Add a root endpoint to redirect to Scalar
            app.MapGet("/", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();

            // Add alternative routes for documentation
            app.MapGet("/docs", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();

            app.MapGet("/api-docs", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            ;

            // Configure the HTTP request pipeline
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();


            app.Run();
        }
    }
}