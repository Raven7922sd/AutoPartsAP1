namespace AutoParts.Shared.DTOs;

public class VentaResponseDto
{
    public int VentaId { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public string? NombreUsuario { get; set; }
    public string? EmailUsuario { get; set; }
    public DateTime Fecha { get; set; }
    public double Total { get; set; }
    public List<VentaDetalleResponseDto> Detalles { get; set; } = new();
    public PagoInfoDto Pago { get; set; } = new();
}

public class VentaDetalleResponseDto
{
    public int DetalleId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public double Cantidad { get; set; }
    public double PrecioUnitario { get; set; }
    public double Subtotal { get; set; }
}

public class PagoInfoDto
{
    public int PagoId { get; set; }
    public string NombreTitular { get; set; } = string.Empty;
    public string NumeroTarjetaEnmascarado { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
}
