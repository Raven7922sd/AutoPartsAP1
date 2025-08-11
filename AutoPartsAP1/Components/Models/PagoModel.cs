using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsAP1.Components.Models;

public class PagoModel
{
    [Key]
    public int PagoId { get; set; }

    [Required(ErrorMessage = "Debe ingresar el nombre del titular")]
    public string NombreTitular { get; set; } = null!;
    [Required(ErrorMessage = "Debe ingresar el número de tarjeta")]

    [StringLength(19, MinimumLength = 13, ErrorMessage = "El número de tarjeta debe tener entre 13 y 19 dígitos")]

    public string NumeroTarjeta { get; set; } = null!;

    [Required(ErrorMessage = "Debe ingresar la fecha de expiración")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Formato de fecha expiración inválido. Use MM/AA")]
    public string FechaExpiracion { get; set; } = null!;

    [Required(ErrorMessage = "Debe ingresar la dirección")]
    public string Direccion { get; set; } = null!;

    [Required(ErrorMessage = "Debe ingresar el código de seguridad CVV")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV inválido, debe tener 3 o 4 dígitos")]
    public string CVV { get; set; } = null!;
}