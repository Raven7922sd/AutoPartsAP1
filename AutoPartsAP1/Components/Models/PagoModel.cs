using System.ComponentModel.DataAnnotations;

namespace AutoPartsAP1.Components.Models;

public class PagoModel
{
    [Key]
    public int PagoId { get; set; }

    [Required(ErrorMessage = "Debe ingresar el nombre del titular")]
    public string NombreTitular { get; set; } = null!;
    [Required(ErrorMessage = "Debe ingresar el número de la tarjeta")]
    public string NumeroTarjeta { get; set; } = null!;

    public string FechaExpiracion { get; set; } = null!;
    [Required(ErrorMessage = "Debe ingresar el la dirección")]
    public string Direccion { get; set; } = null!;
    [Required(ErrorMessage = "Debe ingresar el código de seguridad CVV")]
    public string CVV { get; set; } = null!;
}