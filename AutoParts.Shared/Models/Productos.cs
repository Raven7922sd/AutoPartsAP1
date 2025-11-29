using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AutoParts.Shared.Data;

public class Productos
{
    [Key]
    public int ProductoId { get; set; }

    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(200, ErrorMessage = "Máximo 200 caracteres.")]
    public string ProductoNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Campo obligatorio.")]
    [Range(0.1, 999999999, ErrorMessage = "Máximo 999,999,999 de coste por producto.")]
    public double ProductoMonto { get; set; }

    [Required(ErrorMessage = "Campo obligatorio.")]
    [Range(1, 500000, ErrorMessage = "Debe ingresar una cantidad entre 1 y 500,000.")]
    public double ProductoCantidad { get; set; }

    [Required(ErrorMessage = "Campo obligatorio.")]
    [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
    public string ProductoDescripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La imagen es obligatoria.")]
    [JsonIgnore] // No serializar este campo en JSON del API
    public byte[]? ProductoImagen { get; set; }

    [NotMapped] // No guardar en base de datos
    public string? ProductoImagenBase64 => ProductoImagen != null
        ? Convert.ToBase64String(ProductoImagen)
        : null;

    [NotMapped] // No guardar en base de datos
    public string? ProductoImagenUrl
    {
        get => ProductoImagen != null
            ? $"data:image/png;base64,{Convert.ToBase64String(ProductoImagen)}"
            : null;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Extraer el base64 del data URL si viene en ese formato
                var base64Data = value.Contains(",") ? value.Split(',')[1] : value;
                ProductoImagen = Convert.FromBase64String(base64Data);
            }
        }
    }

    [Required(ErrorMessage = "La elección de categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;

    public DateTime Fecha { get; set; } = DateTime.Now;
}
