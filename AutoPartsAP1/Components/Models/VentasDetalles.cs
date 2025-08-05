    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace AutoPartsAP1.Components.Models;

    public class VentasDetalles
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VentaId { get; set; }


        [ForeignKey("VentaId")]
        public Ventas Venta { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [ForeignKey("ProductoId")]
        public Productos Producto { get; set; }

        [Required]
        [Range(1, 500000, ErrorMessage = "Cantidad fuera de rango.")]
        public double Cantidad { get; set; }

        [Required]
        [Range(0.1, 999999999, ErrorMessage = "Precio inválido.")]
        public double PrecioUnitario { get; set; }
        
        [Required]
        public int PagoId { get; set; }

        [ForeignKey("PagoId")]
        public PagoModel Pago { get; set; }
}