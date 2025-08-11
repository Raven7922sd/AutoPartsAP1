using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsAP1.Components.Models;

public class Carrito
{
    [Key]
    public int CarritoId { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = null!;

    [ForeignKey("Producto")]
    public int ProductoId { get; set; }
    public Productos Producto { get; set; } = null!;

    [Required]
    [Range(1, 100)]
    public int Cantidad { get; set; }
}