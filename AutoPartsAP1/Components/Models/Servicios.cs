using System.ComponentModel.DataAnnotations;

namespace AutoPartsAP1.Components.Models;

public class Servicios
{
    [Key]
    public int ServicioId { get; set; }

    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string Nombre { get; set; } = null!;
    [Required(ErrorMessage = "Campo obligatorio.")]
    [Range(0,99999999, ErrorMessage = "Máximo 99999999 dígitos.")]
    public double Precio { get; set; }
    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string Descripcion { get; set; } = null!;
    [Required(ErrorMessage = "Campo obligatorio.")]
    public double DuracionEstimada { get; set; }

    [Required(ErrorMessage = "La imagen es obligatoria.")]
    public byte[]? ServicioImagen { get; set; }
    public string? ServicioImagenUrl =>
    ServicioImagen != null
    ? $"data:image/png;base64,{Convert.ToBase64String(ServicioImagen)}"
    : null;

    public int Solicitados { get; set; } = 0;

    public DateTime FechaServicio { get; set; }=DateTime.Now;
}