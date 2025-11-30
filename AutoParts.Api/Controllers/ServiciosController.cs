using AutoParts.Shared.Data;
using AutoParts.Shared.DTOs;
using AutoParts.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiciosController : ControllerBase
{
    private readonly ServiciosService _serviciosService;
    private readonly ILogger<ServiciosController> _logger;

    public ServiciosController(ServiciosService serviciosService, ILogger<ServiciosController> logger)
    {
        _serviciosService = serviciosService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los servicios disponibles
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServicioDto>>> GetServicios()
    {
        try
        {
            var servicios = await _serviciosService.Listar(s => s.ServicioId > 0);

            var serviciosDto = servicios.Select(s => new ServicioDto
            {
                ServicioId = s.ServicioId,
                Nombre = s.Nombre,
                Precio = s.Precio,
                Descripcion = s.Descripcion,
                DuracionEstimada = s.DuracionEstimada,
                ServicioImagenBase64 = s.ServicioImagen != null 
                    ? Convert.ToBase64String(s.ServicioImagen) 
                    : null,
                Solicitados = s.Solicitados,
                FechaServicio = s.FechaServicio
            }).ToList();

            return Ok(serviciosDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener los servicios");
            return StatusCode(500, new { message = "Error al obtener los servicios" });
        }
    }

    /// <summary>
    /// Obtiene un servicio específico por ID
    /// </summary>
    [HttpGet("{servicioId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServicioDto>> GetServicio(int servicioId)
    {
        try
        {
            var servicio = await _serviciosService.Buscar(servicioId);

            if (servicio == null)
                return NotFound(new { message = $"Servicio con ID {servicioId} no encontrado" });

            var servicioDto = new ServicioDto
            {
                ServicioId = servicio.ServicioId,
                Nombre = servicio.Nombre,
                Precio = servicio.Precio,
                Descripcion = servicio.Descripcion,
                DuracionEstimada = servicio.DuracionEstimada,
                ServicioImagenBase64 = servicio.ServicioImagen != null
                    ? Convert.ToBase64String(servicio.ServicioImagen)
                    : null,
                Solicitados = servicio.Solicitados,
                FechaServicio = servicio.FechaServicio
            };

            return Ok(servicioDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el servicio {ServicioId}", servicioId);
            return StatusCode(500, new { message = "Error al obtener el servicio" });
        }
    }

    /// <summary>
    /// Busca servicios por nombre
    /// </summary>
    [HttpGet("buscar/{nombre}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServicioDto>>> BuscarPorNombre(string nombre)
    {
        try
        {
            var servicios = await _serviciosService.Listar(s => s.Nombre.Contains(nombre));

            var serviciosDto = servicios.Select(s => new ServicioDto
            {
                ServicioId = s.ServicioId,
                Nombre = s.Nombre,
                Precio = s.Precio,
                Descripcion = s.Descripcion,
                DuracionEstimada = s.DuracionEstimada,
                ServicioImagenBase64 = s.ServicioImagen != null
                    ? Convert.ToBase64String(s.ServicioImagen)
                    : null,
                Solicitados = s.Solicitados,
                FechaServicio = s.FechaServicio
            }).ToList();

            return Ok(serviciosDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar servicios por nombre");
            return StatusCode(500, new { message = "Error al buscar servicios" });
        }
    }

    /// <summary>
    /// Crea un nuevo servicio (Solo Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServicioDto>> CreateServicio([FromBody] CreateServicioDto createServicioDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Convertir imagen de Base64 a bytes
            byte[]? imagenBytes = null;
            if (!string.IsNullOrEmpty(createServicioDto.ServicioImagenBase64))
            {
                try
                {
                    imagenBytes = Convert.FromBase64String(createServicioDto.ServicioImagenBase64);
                }
                catch
                {
                    return BadRequest(new { message = "Formato de imagen inválido" });
                }
            }

            var servicio = new Servicios
            {
                Nombre = createServicioDto.Nombre,
                Precio = createServicioDto.Precio,
                Descripcion = createServicioDto.Descripcion,
                DuracionEstimada = createServicioDto.DuracionEstimada,
                ServicioImagen = imagenBytes,
                Solicitados = 0,
                FechaServicio = DateTime.Now
            };

            var resultado = await _serviciosService.GuardarServicio(servicio);

            if (!resultado)
                return BadRequest(new { message = "Error al crear el servicio" });

            var servicioDto = new ServicioDto
            {
                ServicioId = servicio.ServicioId,
                Nombre = servicio.Nombre,
                Precio = servicio.Precio,
                Descripcion = servicio.Descripcion,
                DuracionEstimada = servicio.DuracionEstimada,
                ServicioImagenBase64 = servicio.ServicioImagen != null
                    ? Convert.ToBase64String(servicio.ServicioImagen)
                    : null,
                Solicitados = servicio.Solicitados,
                FechaServicio = servicio.FechaServicio
            };

            _logger.LogInformation("Servicio {Nombre} creado exitosamente", servicio.Nombre);

            return CreatedAtAction(nameof(GetServicio), new { servicioId = servicio.ServicioId }, servicioDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el servicio");
            return StatusCode(500, new { message = "Error al crear el servicio" });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de los servicios más solicitados
    /// </summary>
    [HttpGet("estadisticas")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetEstadisticas()
    {
        try
        {
            var servicios = await _serviciosService.Listar(s => s.ServicioId > 0);

            var estadisticas = new
            {
                TotalServicios = servicios.Count,
                TotalSolicitados = servicios.Sum(s => s.Solicitados),
                GananciaTotal = _serviciosService.CalcularTotalGanancia(servicios),
                ServiciosMasSolicitados = servicios
                    .OrderByDescending(s => s.Solicitados)
                    .Take(5)
                    .Select(s => new
                    {
                        s.ServicioId,
                        s.Nombre,
                        s.Precio,
                        s.Solicitados,
                        Ganancia = _serviciosService.CalcularGananciaServicio(s)
                    })
                    .ToList()
            };

            return Ok(estadisticas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de servicios");
            return StatusCode(500, new { message = "Error al obtener estadísticas" });
        }
    }
}
