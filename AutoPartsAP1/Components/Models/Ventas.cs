using AutoPartsAP1.Data;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoPartsAP1.Components.Models;
public class Ventas
{
    [Key]
    public int VentaId { get; set; }

    public DateTime Fecha { get; set; } =DateTime.Now;

    [Required(ErrorMessage ="Debe ingresar un usuario")]
    public string ApplicationUserId { get; set; }

    [ForeignKey("ApplicationUserId")]
    public ApplicationUser Usuario { get; set; }

    [InverseProperty("Venta")]
    public List<VentasDetalles> VentasDetalles { get; set; } = new();

    public double Total { get; set; }
}