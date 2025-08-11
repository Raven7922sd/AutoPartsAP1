namespace AutoPartsAP1.Components.Models;

public class FacturaDto
{
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public string UsuarioEmail { get; set; }
    public double Total { get; set; }
    public List<FacturaDetalleDto> Detalles { get; set; } = new();
}
