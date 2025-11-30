namespace AutoParts.Shared.DTOs;

public class LoginResponse
{
    public string TokenType { get; set; } = "Bearer";
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}
