namespace AutoPartsAP1.Components.Models;

public class Carrito
{
    public Productos Producto { get; set; } = new Productos();
    public int Cantidad { get; set; }

    public double Subtotal => Producto.ProductoMonto * Cantidad;
}