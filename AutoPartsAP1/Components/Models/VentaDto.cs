namespace AutoPartsAP1.Components.Models;

public class VentaDto
{
    public int VentaId { get; set; }
    public DateTime Fecha { get; set; }
    public Double Total { get; set; }

    public List<VentaDetalleDto> Detalles { get; set; } = new();
}
