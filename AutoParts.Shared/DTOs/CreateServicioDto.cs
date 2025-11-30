using System.ComponentModel.DataAnnotations;

namespace AutoParts.Shared.DTOs;

public class CreateServicioDto
{
    [Required(ErrorMessage = "El nombre del servicio es obligatorio")]
    [StringLength(500, ErrorMessage = "El nombre no puede exceder 500 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0, 99999999, ErrorMessage = "El precio debe estar entre 0 y 99999999")]
    public double Precio { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria")]
    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La duración estimada es obligatoria")]
    [Range(0.1, 24, ErrorMessage = "La duración debe estar entre 0.1 y 24 horas")]
    public double DuracionEstimada { get; set; }

    [Required(ErrorMessage = "La imagen es obligatoria")]
    public string ServicioImagenBase64 { get; set; } = string.Empty;
}
