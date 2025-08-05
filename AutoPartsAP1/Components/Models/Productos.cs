using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsAP1.Components.Models;
public class Productos
{
    [Key]
    public int ProductoId { get; set; }

    [Required(ErrorMessage ="Campo obligatorio.")]
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
    public byte[]? ProductoImagen { get; set; }
    public string? ProductoImagenUrl =>
    ProductoImagen != null
        ? $"data:image/png;base64,{Convert.ToBase64String(ProductoImagen)}"
        : null;
    [Required(ErrorMessage = "La elección de categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }= DateTime.Now;
}