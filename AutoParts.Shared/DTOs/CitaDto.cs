namespace AutoParts.Shared.DTOs;

public class CitaDto
{
    public int CitaId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ApplicationUserId { get; set; } = string.Empty;
    public string ServicioSolicitado { get; set; } = string.Empty;
    public DateTime FechaCita { get; set; }
    public bool Confirmada { get; set; }
    public string CodigoConfirmacion { get; set; } = string.Empty;
}
