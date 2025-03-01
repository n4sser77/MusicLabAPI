namespace Backend.asp.Services.Interfaces;
public interface IJwtService
{

    public Task<string> GenerateJwtToken(string userId, string role);

    public Dictionary<string, string> ValidateToken(string token);
}