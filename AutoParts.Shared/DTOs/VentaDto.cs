namespace AutoParts.Shared.DTOs;

public class VentaDto
{
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public double Total { get; set; }
    public List<VentaDetalleDto> Detalles { get; set; } = new();
}
