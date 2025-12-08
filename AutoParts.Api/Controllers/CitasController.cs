using AutoParts.Shared.Data;
using AutoParts.Shared.DTOs;
using AutoParts.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CitasController : ControllerBase
{
    private readonly CitaService _citaService;
    private readonly ServiciosService _serviciosService;
    private readonly ILogger<CitasController> _logger;

    public CitasController(
        CitaService citaService,
        ServiciosService serviciosService,
        ILogger<CitasController> logger)
    {
        _citaService = citaService;
        _serviciosService = serviciosService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    /// <summary>
    /// Obtiene todas las citas del usuario autenticado
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CitaDto>>> GetMisCitas()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Usuario no autenticado" });

            var citas = await _citaService.GetCitasPorUsuarioIdAsync(userId);

            var citasDto = citas.Select(c => new CitaDto
            {
                CitaId = c.CitaId,
                ClienteNombre = c.ClienteNombre,
                ApplicationUserId = c.ApplicationUserId,
                ServicioSolicitado = c.ServicioSolicitado,
                FechaCita = c.FechaCita,
                Confirmada = c.Confirmada,
                CodigoConfirmacion = c.CodigoConfirmacion
            }).ToList();

            return Ok(citasDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener las citas del usuario");
            return StatusCode(500, new { message = "Error al obtener las citas" });
        }
    }

    /// <summary>
    /// Obtiene una cita específica por ID
    /// </summary>
    [HttpGet("{citaId}")]
    public async Task<ActionResult<CitaDto>> GetCita(int citaId)
    {
        try
        {
            var userId = GetUserId();
            var cita = await _citaService.GetCitaByIdAsync(citaId);

            if (cita == null)
                return NotFound(new { message = $"Cita con ID {citaId} no encontrada" });

            // Verificar que la cita pertenece al usuario (excepto si es Admin)
            if (cita.ApplicationUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var citaDto = new CitaDto
            {
                CitaId = cita.CitaId,
                ClienteNombre = cita.ClienteNombre,
                ApplicationUserId = cita.ApplicationUserId,
                ServicioSolicitado = cita.ServicioSolicitado,
                FechaCita = cita.FechaCita,
                Confirmada = cita.Confirmada,
                CodigoConfirmacion = cita.CodigoConfirmacion
            };

            return Ok(citaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la cita {CitaId}", citaId);
            return StatusCode(500, new { message = "Error al obtener la cita" });
        }
    }

    /// <summary>
    /// Crea una nueva cita para un servicio
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CitaDto>> CreateCita([FromBody] CreateCitaDto createCitaDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Usuario no autenticado" });

            // Validar que el servicio existe
            var servicio = await _serviciosService.BuscarPorNombre(createCitaDto.ServicioSolicitado);
            if (servicio == null)
                return BadRequest(new { message = "El servicio solicitado no existe" });

            // Validar que la fecha de la cita sea futura
            if (createCitaDto.FechaCita <= DateTime.Now)
                return BadRequest(new { message = "La fecha de la cita debe ser futura" });

            var cita = new Cita
            {
                ClienteNombre = createCitaDto.ClienteNombre,
                ApplicationUserId = userId,
                ServicioSolicitado = createCitaDto.ServicioSolicitado,
                FechaCita = createCitaDto.FechaCita,
                Confirmada = false
            };

            var resultado = await _citaService.GuardarCitaAsync(cita);

            if (!resultado)
                return BadRequest(new { message = "Error al crear la cita" });

            // Recargar la cita para obtener el código de confirmación
            var citaCreada = await _citaService.GetCitaByIdAsync(cita.CitaId);

            var citaDto = new CitaDto
            {
                CitaId = citaCreada!.CitaId,
                ClienteNombre = citaCreada.ClienteNombre,
                ApplicationUserId = citaCreada.ApplicationUserId,
                ServicioSolicitado = citaCreada.ServicioSolicitado,
                FechaCita = citaCreada.FechaCita,
                Confirmada = citaCreada.Confirmada,
                CodigoConfirmacion = citaCreada.CodigoConfirmacion
            };

            _logger.LogInformation("Cita creada para usuario {UserId} con código {CodigoConfirmacion}", 
                userId, citaCreada.CodigoConfirmacion);

            return CreatedAtAction(nameof(GetCita), new { citaId = cita.CitaId }, citaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear la cita");
            return StatusCode(500, new { message = "Error al crear la cita" });
        }
    }

    /// <summary>
    /// Actualiza una cita existente
    /// </summary>
    [HttpPut("{citaId}")]
    public async Task<ActionResult> UpdateCita(int citaId, [FromBody] UpdateCitaDto updateCitaDto)
    {
        try
        {
            var userId = GetUserId();
            var cita = await _citaService.GetCitaByIdAsync(citaId);

            if (cita == null)
                return NotFound(new { message = $"Cita con ID {citaId} no encontrada" });

            // Verificar que la cita pertenece al usuario (excepto si es Admin)
            if (cita.ApplicationUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // No permitir modificar citas ya confirmadas
            if (cita.Confirmada && !User.IsInRole("Admin"))
                return BadRequest(new { message = "No se puede modificar una cita confirmada" });

            // Actualizar solo los campos que se enviaron
            if (!string.IsNullOrEmpty(updateCitaDto.ClienteNombre))
                cita.ClienteNombre = updateCitaDto.ClienteNombre;

            if (!string.IsNullOrEmpty(updateCitaDto.ServicioSolicitado))
            {
                // Validar que el nuevo servicio existe
                var servicio = await _serviciosService.BuscarPorNombre(updateCitaDto.ServicioSolicitado);
                if (servicio == null)
                    return BadRequest(new { message = "El servicio solicitado no existe" });

                cita.ServicioSolicitado = updateCitaDto.ServicioSolicitado;
            }

            if (updateCitaDto.FechaCita.HasValue)
            {
                if (updateCitaDto.FechaCita.Value <= DateTime.Now)
                    return BadRequest(new { message = "La fecha de la cita debe ser futura" });

                cita.FechaCita = updateCitaDto.FechaCita.Value;
            }

            if (updateCitaDto.Confirmada.HasValue && User.IsInRole("Admin"))
                cita.Confirmada = updateCitaDto.Confirmada.Value;

            var resultado = await _citaService.GuardarCitaAsync(cita);

            if (!resultado)
                return BadRequest(new { message = "Error al actualizar la cita" });

            _logger.LogInformation("Cita {CitaId} actualizada por usuario {UserId}", citaId, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar la cita {CitaId}", citaId);
            return StatusCode(500, new { message = "Error al actualizar la cita" });
        }
    }

    /// <summary>
    /// Confirma una cita (Solo Admin o dueño de la cita)
    /// </summary>
    [HttpPost("{citaId}/confirmar")]
    public async Task<ActionResult<CitaDto>> ConfirmarCita(int citaId)
    {
        try
        {
            var userId = GetUserId();
            var cita = await _citaService.GetCitaByIdAsync(citaId);

            if (cita == null)
                return NotFound(new { message = $"Cita con ID {citaId} no encontrada" });

            // Verificar permisos
            if (cita.ApplicationUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            if (cita.Confirmada)
                return BadRequest(new { message = "La cita ya está confirmada" });

            var resultado = await _citaService.ConfirmarCitaAsync(citaId);

            if (!resultado)
                return BadRequest(new { message = "Error al confirmar la cita" });

            // Recargar la cita para obtener el estado actualizado
            var citaActualizada = await _citaService.GetCitaByIdAsync(citaId);

            var citaDto = new CitaDto
            {
                CitaId = citaActualizada!.CitaId,
                ClienteNombre = citaActualizada.ClienteNombre,
                ApplicationUserId = citaActualizada.ApplicationUserId,
                ServicioSolicitado = citaActualizada.ServicioSolicitado,
                FechaCita = citaActualizada.FechaCita,
                Confirmada = citaActualizada.Confirmada,
                CodigoConfirmacion = citaActualizada.CodigoConfirmacion
            };

            _logger.LogInformation("Cita {CitaId} confirmada por usuario {UserId}", citaId, userId);

            return Ok(citaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al confirmar la cita {CitaId}", citaId);
            return StatusCode(500, new { message = "Error al confirmar la cita" });
        }
    }

    /// <summary>
    /// Cancela/elimina una cita
    /// </summary>
    [HttpDelete("{citaId}")]
    public async Task<ActionResult> CancelarCita(int citaId)
    {
        try
        {
            var userId = GetUserId();
            var cita = await _citaService.GetCitaByIdAsync(citaId);

            if (cita == null)
                return NotFound(new { message = $"Cita con ID {citaId} no encontrada" });

            // Verificar permisos
            if (cita.ApplicationUserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // No permitir cancelar citas confirmadas a menos que sea Admin
            if (cita.Confirmada && !User.IsInRole("Admin"))
                return BadRequest(new { message = "No se puede cancelar una cita confirmada. Contacte al administrador." });

            var resultado = await _citaService.EliminarCitaAsync(cita);

            if (!resultado)
                return BadRequest(new { message = "Error al cancelar la cita" });

            _logger.LogInformation("Cita {CitaId} cancelada por usuario {UserId}", citaId, userId);

            return Ok(new { message = "Cita cancelada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar la cita {CitaId}", citaId);
            return StatusCode(500, new { message = "Error al cancelar la cita" });
        }
    }

    /// <summary>
    /// Obtiene todas las citas (Solo Admin)
    /// </summary>
    [HttpGet("todas")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetTodasLasCitas([FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 10)
    {
        try
        {
            var resultado = await _citaService.BuscarCitasAsync("", "", null, null, pagina, tamanoPagina);

            var citasDto = resultado.Items.Select(c => new CitaDto
            {
                CitaId = c.CitaId,
                ClienteNombre = c.ClienteNombre,
                ApplicationUserId = c.ApplicationUserId,
                ServicioSolicitado = c.ServicioSolicitado,
                FechaCita = c.FechaCita,
                Confirmada = c.Confirmada,
                CodigoConfirmacion = c.CodigoConfirmacion
            }).ToList();

            return Ok(new
            {
                citas = citasDto,
                totalPaginas = resultado.TotalPaginas,
                paginaActual = resultado.PaginaActual
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las citas");
            return StatusCode(500, new { message = "Error al obtener las citas" });
        }
    }

    /// <summary>
    /// Busca citas con filtros (Solo Admin)
    /// </summary>
    [HttpGet("buscar")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> BuscarCitas(
        [FromQuery] string? filtro,
        [FromQuery] string? valorFiltro,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10)
    {
        try
        {
            var resultado = await _citaService.BuscarCitasAsync(
                filtro ?? "",
                valorFiltro ?? "",
                fechaDesde,
                fechaHasta,
                pagina,
                tamanoPagina);

            var citasDto = resultado.Items.Select(c => new CitaDto
            {
                CitaId = c.CitaId,
                ClienteNombre = c.ClienteNombre,
                ApplicationUserId = c.ApplicationUserId,
                ServicioSolicitado = c.ServicioSolicitado,
                FechaCita = c.FechaCita,
                Confirmada = c.Confirmada,
                CodigoConfirmacion = c.CodigoConfirmacion
            }).ToList();

            return Ok(new
            {
                citas = citasDto,
                totalPaginas = resultado.TotalPaginas,
                paginaActual = resultado.PaginaActual
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar citas");
            return StatusCode(500, new { message = "Error al buscar citas" });
        }
    }
}
