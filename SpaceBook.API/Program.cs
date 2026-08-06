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

 
var builder = WebApplication.CreateBuilder(args);
 
// Add Controllers
builder.Services.AddControllers();
 
// PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));
 
// Dependency Injection
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<
    IMissedCheckInRepository,
    MissedCheckInRepository>();

builder.Services.AddScoped<
    MissedCheckInService>();

    
// JWT Authentication
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
        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

    ClockSkew = TimeSpan.Zero
};
});
 
builder.Services.AddAuthorization();
 builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEmployeeBookingRepository, EmployeeBookingRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<
    IEmployeeCheckInService,
    EmployeeCheckInService>();

builder.Services.AddScoped<
    IEmployeeCheckInRepository,
    EmployeeCheckInRepository>();

builder.Services.AddScoped<
    INotificationRepository,
    NotificationRepository>();

builder.Services.AddScoped<MissedCheckInService>();


builder.Services.AddScoped<IEmployeeBookingService, EmployeeBookingService>();
builder.Services.AddScoped<
    IEmployeeDashboardRepository,
    EmployeeDashboardRepository>();
 
builder.Services.AddScoped<
    IEmployeeDashboardService,
    EmployeeDashboardService>();
// Swagger
builder.Services.AddEndpointsApiExplorer();
 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SpaceBook API",
        Version = "v1"
    });
 
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
 
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
 
var app = builder.Build();
 
// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
 
app.UseHttpsRedirection();
 
app.UseAuthentication();
 
app.UseAuthorization();
 
app.MapControllers();
 
app.Run();