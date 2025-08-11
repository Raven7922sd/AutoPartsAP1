namespace AutoPartsAP1.Components.Models;

public class FacturaDetalleDto
{
    public string ProductoNombre { get; set; }
    public string Categoria { get; set; }
    public double PrecioUnitario { get; set; }
    public double Cantidad { get; set; }
    public double Subtotal { get; set; }
}
