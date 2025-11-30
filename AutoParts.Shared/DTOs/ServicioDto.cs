namespace AutoParts.Shared.DTOs;

public class ServicioDto
{
    public int ServicioId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public double Precio { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public double DuracionEstimada { get; set; }
    public string? ServicioImagenBase64 { get; set; }
    public int Solicitados { get; set; }
    public DateTime FechaServicio { get; set; }
}
