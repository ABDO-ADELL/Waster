using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;
using Waster.Helpers;
using Waster.Hubs;
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

            // Configure JWT settings
            builder.Services.Configure<Jwt>(builder.Configuration.GetSection("Jwt"));

            // Configure Identity
            builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
                options.Lockout.MaxFailedAccessAttempts = 6;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Database Context
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<IBookMarkRepository, BookMarkRepository>();
            builder.Services.AddScoped<IBrowseRepository, BrowseRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddTransient(typeof(IBaseReporesitory<>), typeof(BaseReporesitory<>));
            builder.Services.AddScoped<INotificationService, NotificationService>();


            // Add SignalR
            builder.Services.AddSignalR();

            // Configure Authentication (JWT + Google OAuth)
            builder.Services.AddAuthentication(options =>
            {
                // JWT is the default for API endpoints
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production if using HTTPS
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
                    ),
                    ClockSkew = TimeSpan.Zero // Remove default 5 min clock skew
                };
            })
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                googleOptions.SaveTokens = true;
                googleOptions.CallbackPath = "/signin-google";
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? new[] { "http://localhost:3000" };

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Configure JSON options globally for OpenAPI
            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.SerializerOptions.MaxDepth = 256;
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

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Static Files with CORS
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=600");
                }
            });

            // Enable CORS
            app.UseCors("AllowFrontend");

            // HTTPS Redirection
            app.UseHttpsRedirection();

            // Routing
            app.UseRouting();

            // Authentication & Authorization (Order matters!)
            app.UseAuthentication();
            app.UseAuthorization();

            // Map Controllers
            app.MapHub<NotificationHub>("/notificationHub");
            app.MapControllers();


            // Map OpenAPI endpoint
            app.MapOpenApi();

            // Map Scalar API Documentation
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Waster API Documentation")
                    .WithTheme(ScalarTheme.Mars)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });

            // Root endpoint redirects to documentation
            app.MapGet("/", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();

            app.MapGet("/docs", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();

            app.MapGet("/api-docs", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();

            app.Run();
        }
    }
}