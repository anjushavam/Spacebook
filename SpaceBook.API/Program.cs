using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpaceBook.Application.Common.JsonConverters;
using SpaceBook.Application.Interfaces;
using SpaceBook.Application.Services;
using SpaceBook.Infrastructure.Authentication;
using SpaceBook.Infrastructure.BackgroundServices;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Infrastructure.Repositories;
using SpaceBook.Infrastructure.Services;
using SpaceBook.API.Middleware;
using System.Reflection;
using System.Text;

// =====================================================
// Render / Linux configuration
// Disable configuration file reload BEFORE creating builder
// Prevents FileSystemWatcher / inotify issues on Render
// =====================================================

Environment.SetEnvironmentVariable(
    "DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE",
    "false"
);

// =====================================================
// Create Builder
// =====================================================

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// Controllers & JSON Serialization
// =====================================================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableTimeOnlyJsonConverter());
    });

// =====================================================
// PostgreSQL / Entity Framework Core
// =====================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is not configured."
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// =====================================================
// CORS
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://spacebookss.netlify.app",
                "https://spacebook211-fcms-1bpe04jye-dvikash211-5241s-projects.vercel.app",
                "https://spacebook-frontend-v64o.onrender.com",
                "https://spacebooks.duckdns.org",
                "https://spacebook-u1qc.onrender.com",
                "https://spacebook211-98989-cs4tdldnv-dvikash211-5241s-projects.vercel.app",
                "https://spacebook211-98989-acn15v4y5-dvikash211-5241s-projects.vercel.app",
                "https://spacebook211-98989-g6itccmhu-dvikash211-5241s-projects.vercel.app"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =====================================================
// Dependency Injection - Authentication
// =====================================================

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// =====================================================
// Dependency Injection - Check In
// =====================================================

builder.Services.AddScoped<IMissedCheckInRepository, MissedCheckInRepository>();
builder.Services.AddScoped<IEmployeeCheckInRepository, EmployeeCheckInRepository>();
builder.Services.AddScoped<IEmployeeCheckInService, EmployeeCheckInService>();
builder.Services.AddScoped<IHotseatRepository, HotseatRepository>();

// =====================================================
// JWT Authentication
// =====================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured."
    );
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "Jwt:Issuer is not configured."
    );
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "Jwt:Audience is not configured."
    );
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

            ClockSkew = TimeSpan.Zero
        };
});

builder.Services.AddAuthorization();

// =====================================================
// Admin
// =====================================================

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();

// =====================================================
// Room
// =====================================================

builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();

// =====================================================
// Email Service
// =====================================================

builder.Services.AddScoped<IEmailService, EmailService>();

// =====================================================
// Notification
// =====================================================

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// =====================================================
// Booking Reminders & Background Service
// =====================================================

builder.Services.AddScoped<IBookingReminderRepository, BookingReminderRepository>();
builder.Services.AddScoped<IBookingReminderService, BookingReminderService>();
builder.Services.AddHostedService<BookingReminderBackgroundService>();

// =====================================================
// Booking
// =====================================================

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();
builder.Services.AddScoped<IFacilityService, FacilityService>();

// =====================================================
// Employee Booking
// =====================================================

builder.Services.AddScoped<IEmployeeBookingRepository, EmployeeBookingRepository>();
builder.Services.AddScoped<IEmployeeBookingService, EmployeeBookingService>();

// =====================================================
// Reports
// =====================================================

builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

// =====================================================
// Copilot
// =====================================================

builder.Services.AddScoped<ICopilotRepository, CopilotRepository>();
builder.Services.AddScoped<ICopilotService, CopilotService>();

// =====================================================
// Employee Dashboard
// =====================================================

builder.Services.AddScoped<IEmployeeDashboardRepository, EmployeeDashboardRepository>();
builder.Services.AddScoped<IEmployeeDashboardService, EmployeeDashboardService>();

// =====================================================
// Swagger
// =====================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SpaceBook API",
        Version = "v1"
    });

    // =================================================
    // JWT Bearer Authentication
    // =================================================

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "Enter JWT token. Example: Bearer {token}",

            Name = "Authorization",
            In = ParameterLocation.Header,

            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        }
    );

    // =================================================
    // Copilot API Key
    // =================================================

    c.AddSecurityDefinition(
        "CopilotApiKey",
        new OpenApiSecurityScheme
        {
            Description =
                "Enter the Copilot API key.",

            Name = "X-Copilot-Key",
            In = ParameterLocation.Header,

            Type = SecuritySchemeType.ApiKey
        }
    );

    // =================================================
    // IMPORTANT
    //
    // Add security requirements PER ENDPOINT.
    //
    // [Authorize] endpoints
    //     -> Bearer
    //
    // /api/copilot/* endpoints
    //     -> CopilotApiKey
    //
    // [AllowAnonymous] endpoints
    //     -> No lock
    // =================================================

    c.OperationFilter<SwaggerSecurityOperationFilter>();
});

// =====================================================
// Build Application
// =====================================================

var app = builder.Build();

// =====================================================
// Swagger
// =====================================================

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "SpaceBook API v1"
    );
});

// =====================================================
// HTTPS
// =====================================================

// Render handles HTTPS at the proxy level.
// Only redirect locally.

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// =====================================================
// CORS
// =====================================================

app.UseCors("AllowReactApp");

// =====================================================
// Copilot API Key Authentication
// =====================================================

app.UseMiddleware<CopilotApiKeyMiddleware>();

// =====================================================
// Authentication & Authorization
// =====================================================

app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// Controllers
// =====================================================

app.MapControllers();

// =====================================================
// Run
// =====================================================

app.Run();


// =====================================================
// Swagger Security Operation Filter
// =====================================================
//
// This tells Swagger which authentication method belongs
// to each individual API operation.
//
// JWT:
//     [Authorize] endpoints
//
// Copilot:
//     /api/copilot/* endpoints
//
// Public:
//     [AllowAnonymous] endpoints
//
// =====================================================

public class SwaggerSecurityOperationFilter
    : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(
        Microsoft.OpenApi.Models.OpenApiOperation operation,
        Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        var relativePath =
            apiDescription.RelativePath?
                .TrimStart('/')
                .ToLowerInvariant();

        // =================================================
        // Check whether this is a Copilot endpoint
        // =================================================

        var isCopilotEndpoint =
            relativePath != null &&
            relativePath.StartsWith("api/copilot/");

        // =================================================
        // Check AllowAnonymous
        // =================================================

        var endpointMetadata =
            apiDescription.ActionDescriptor.EndpointMetadata;

        var allowAnonymous =
            endpointMetadata
                .OfType<AllowAnonymousAttribute>()
                .Any();

        // =================================================
        // Copilot API
        // =================================================

        if (isCopilotEndpoint)
        {
            operation.Security =
                new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference =
                                    new OpenApiReference
                                    {
                                        Type =
                                            ReferenceType.SecurityScheme,

                                        Id =
                                            "CopilotApiKey"
                                    }
                            },
                            Array.Empty<string>()
                        }
                    }
                };

            return;
        }

        // =================================================
        // Public endpoint
        // =================================================

        if (allowAnonymous)
        {
            operation.Security = null;
            return;
        }

        // =================================================
        // Check [Authorize]
        // =================================================

        var hasAuthorize =
            endpointMetadata
                .OfType<IAuthorizeData>()
                .Any();

        // =================================================
        // JWT protected endpoint
        // =================================================

        if (hasAuthorize)
        {
            operation.Security =
                new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference =
                                    new OpenApiReference
                                    {
                                        Type =
                                            ReferenceType.SecurityScheme,

                                        Id =
                                            "Bearer"
                                    }
                            },
                            Array.Empty<string>()
                        }
                    }
                };

            return;
        }

        // =================================================
        // No authentication
        // =================================================

        operation.Security = null;
    }
}