namespace AutoPartsAP1.Components.Models;

public class VentaDetalleDto
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; }
    public double Precio { get; set; }
    public double Cantidad { get; set; }

    public string Direccion { get; set; }
}
