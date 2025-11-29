namespace AutoParts.Shared.DTOs;

public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
