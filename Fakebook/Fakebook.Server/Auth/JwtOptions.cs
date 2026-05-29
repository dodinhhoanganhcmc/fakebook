namespace Fakebook.Server.Auth;

public class JwtOptions
{
    public string Issuer   { get; set; } = "fakebook";
    public string Audience { get; set; } = "fakebook-clients";
    public string Secret   { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays   { get; set; } = 14;
}
