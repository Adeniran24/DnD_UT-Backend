using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using GameApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GameApi.Tests.Services;

public class JwtServiceTests
{
    private const string JwtKey = "Aa1!Bb2@Cc3#Dd4$Ee5%Ff6^Gg7&Hh8*";
    private const string Issuer = "dnd-tool";
    private const string Audience = "dnd-tool";

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            })
            .Build();

    [Fact(DisplayName = "Generate Token Includes Expected Claims.")]
    public void GenerateToken_IncludesExpectedClaims()
    {
        var service = new JwtService(CreateConfiguration());
        var token = service.GenerateToken(42, "user@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("user@example.com", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);

        var expectedExpiry = DateTime.UtcNow.AddHours(1);
        Assert.InRange(jwt.ValidTo, expectedExpiry.AddSeconds(-30), expectedExpiry.AddSeconds(30));
    }

    [Fact(DisplayName = "Generate Token Can Be Validated With Configuration Key.")]
    public void GenerateToken_CanBeValidatedWithConfigurationKey()
    {
        var service = new JwtService(CreateConfiguration());
        var token = service.GenerateToken(99, "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

        var jwtToken = Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal("99", jwtToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("test@example.com", jwtToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.NotNull(principal);
    }
}
