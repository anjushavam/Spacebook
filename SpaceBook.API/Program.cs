using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SpaceBook.Application.Interfaces;
using SpaceBook.Application.Services;
using SpaceBook.Infrastructure.Authentication;
using SpaceBook.Infrastructure.Data;
using SpaceBook.Infrastructure.Repositories;
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
// Controllers
// =====================================================

builder.Services.AddControllers();

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
// Notification
// =====================================================

builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// =====================================================
// Booking
// =====================================================

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

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

    // JWT Bearer
    c.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "Enter JWT token. Example: Bearer {token}",

            Name = "Authorization",
            In = ParameterLocation.Header,

            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

// =====================================================
// Build Application
// =====================================================

var app = builder.Build();

// =====================================================
// Swagger
// =====================================================

app.UseSwagger();
app.UseSwaggerUI();

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