using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsAP1.Components.Models;
public class Ventas
{
    [Key]
    public int VentaId { get; set; }

    public DateTime Fecha { get; set; } =DateTime.Now;

    [InverseProperty("Venta")]
    public virtual ICollection<VentasDetalles> VentasDetalles { get; set; } = new List<VentasDetalles>();
}