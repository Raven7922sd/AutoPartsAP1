using System.ComponentModel.DataAnnotations;

namespace AutoParts.Shared.DTOs;

public class PagoDto
{
    [Required(ErrorMessage = "El nombre del titular es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string NombreTitular { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de tarjeta es requerido")]
    [StringLength(19, MinimumLength = 13, ErrorMessage = "El número de tarjeta debe tener entre 13 y 19 dígitos")]
    public string NumeroTarjeta { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de expiración es requerida")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Formato inválido. Use MM/AA")]
    public string FechaExpiracion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El CVV es requerido")]
    [StringLength(4, MinimumLength = 3, ErrorMessage = "El CVV debe tener 3 o 4 dígitos")]
    public string CVV { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dirección es requerida")]
    [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
    public string Direccion { get; set; } = string.Empty;
}
