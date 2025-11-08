using Waster.Controllers;
using Waster.Helpers;
using Waster.Models;
using Waster.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Server;
using Scalar.AspNetCore;
using System.Configuration;
using System.Text.Json.Serialization; // Add this


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


            app.UseStaticFiles(); // This allows accessing /uploads/images/abc123.jpg

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