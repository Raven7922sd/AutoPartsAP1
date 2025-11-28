namespace AutoParts.Shared.DTOs;

public class ProductoDto
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public double ProductoMonto { get; set; }
    public double ProductoCantidad { get; set; }
    public string ProductoDescripcion { get; set; } = string.Empty;
    public string? ProductoImagenBase64 { get; set; }
    public string? ProductoImagenUrl { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
