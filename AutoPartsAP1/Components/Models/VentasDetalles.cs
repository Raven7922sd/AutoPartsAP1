using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsAP1.Components.Models;

public class VentasDetalles
{
    [Key]
    public int DetalleId { get; set; }

    public double Monto { get; set; }

    public int Cantidad { get; set; }

    public double ValorCobrado { get; set; }

    public int VentaId { get; set; }
    [ForeignKey("VentaId")]
    [InverseProperty("VentasDetalles")]
    public virtual Ventas Venta { get; set; } = null!;
}