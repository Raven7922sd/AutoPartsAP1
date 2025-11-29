using System.ComponentModel.DataAnnotations;

namespace AutoParts.Shared.DTOs;

public class CreateUserDto
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
}
