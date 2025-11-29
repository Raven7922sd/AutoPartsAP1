namespace AutoParts.Shared.DTOs;

public class CarritoDto
{
    public int CarritoId { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public int ProductoId { get; set; }
    public ProductoDto? Producto { get; set; }
    public int Cantidad { get; set; }
}
