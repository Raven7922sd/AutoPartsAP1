using System.ComponentModel.DataAnnotations;

namespace AutoParts.Shared.DTOs;

public class UpdateCitaDto
{
    [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string? ClienteNombre { get; set; }

    public string? ServicioSolicitado { get; set; }

    public DateTime? FechaCita { get; set; }

    public bool? Confirmada { get; set; }
}
