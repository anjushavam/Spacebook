using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SpaceBook.Application.Interfaces;
using SpaceBook.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpaceBook.Infrastructure.Authentication;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;


    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public string GenerateToken(Employee employee)
    {
        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                employee.EmployeeId.ToString()),

            new Claim(
                ClaimTypes.Name,
                employee.Name),

            new Claim(
                ClaimTypes.Email,
                employee.Email),

            new Claim(
                ClaimTypes.Role,
                employee.Role!.RoleName)
        };


        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!)
        );


        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);


        var token = new JwtSecurityToken(
            issuer:
                _configuration["Jwt:Issuer"],

            audience:
                _configuration["Jwt:Audience"],

            claims:
                claims,

            expires:
                DateTime.UtcNow.AddMinutes(15),

            signingCredentials:
                credentials
        );


        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}