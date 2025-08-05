using System.ComponentModel.DataAnnotations;
using AutoPartsAP1.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsAP1.Components.Models;

public class Cita
{
    [Key]
    public int CitaId { get; set; }

    [Required]
    public string ClienteNombre { get; set; }

    [Required(ErrorMessage = "Debe ingresar un usuario")]
    public string ApplicationUserId { get; set; }

    [ForeignKey("ApplicationUserId")]
    public ApplicationUser Usuario { get; set; }

    [Required]
    public string ServicioSolicitado { get; set; }

    [Required]
    public DateTime FechaCita { get; set; }=DateTime.Now;

    public bool Confirmada { get; set; } = false;

    public string CodigoConfirmacion { get; set; }
}
