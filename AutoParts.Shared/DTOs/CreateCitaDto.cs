using System.ComponentModel.DataAnnotations;

namespace AutoParts.Shared.DTOs;

public class CreateCitaDto
{
    [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string ClienteNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El servicio solicitado es obligatorio")]
    public string ServicioSolicitado { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de la cita es obligatoria")]
    public DateTime FechaCita { get; set; }
}
