using AutoParts.Shared.Data;
using AutoParts.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VentasController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VentasController> _logger;

    public VentasController(ApplicationDbContext context, ILogger<VentasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private static string EnmascararTarjeta(string numeroTarjeta)
    {
        if (string.IsNullOrEmpty(numeroTarjeta) || numeroTarjeta.Length < 4)
            return "****";

        return $"**** **** **** {numeroTarjeta[^4..]}";
    }

    /// <summary>
    /// Obtiene todas las ventas del usuario autenticado
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<VentaResponseDto>>> GetVentas()
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Usuario no autenticado");

            var ventas = await _context.Ventas
                .Where(v => v.ApplicationUserId == userId)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Pago)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            var ventasDto = ventas.Select(v => new VentaResponseDto
            {
                VentaId = v.VentaId,
                ApplicationUserId = v.ApplicationUserId,
                Fecha = v.Fecha,
                Total = v.Total,
                Detalles = v.VentasDetalles.Select(d => new VentaDetalleResponseDto
                {
                    DetalleId = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.ProductoNombre ?? "Producto no disponible",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Cantidad * d.PrecioUnitario
                }).ToList(),
                Pago = v.VentasDetalles.FirstOrDefault()?.Pago != null
                    ? new PagoInfoDto
                    {
                        PagoId = v.VentasDetalles.First().Pago.PagoId,
                        NombreTitular = v.VentasDetalles.First().Pago.NombreTitular,
                        NumeroTarjetaEnmascarado = EnmascararTarjeta(v.VentasDetalles.First().Pago.NumeroTarjeta),
                        Direccion = v.VentasDetalles.First().Pago.Direccion
                    }
                    : new PagoInfoDto()
            }).ToList();

            return Ok(ventasDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener las ventas");
            return StatusCode(500, "Error al obtener las ventas");
        }
    }

    /// <summary>
    /// Obtiene todas las ventas de todos los usuarios (Solo Admin)
    /// </summary>
    [HttpGet("todas")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetTodasLasVentas(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 10,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] string? usuarioId = null)
    {
        try
        {
            var query = _context.Ventas
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Pago)
                .Include(v => v.Usuario)
                .AsQueryable();

            // Filtrar por usuario si se proporciona
            if (!string.IsNullOrEmpty(usuarioId))
                query = query.Where(v => v.ApplicationUserId == usuarioId);

            // Filtrar por rango de fechas
            if (fechaDesde.HasValue)
                query = query.Where(v => v.Fecha >= fechaDesde.Value);
            
            if (fechaHasta.HasValue)
                query = query.Where(v => v.Fecha <= fechaHasta.Value);

            query = query.OrderByDescending(v => v.Fecha);

            var totalVentas = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalVentas / (double)tamanoPagina);

            var ventas = await query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            var ventasDto = ventas.Select(v => new VentaResponseDto
            {
                VentaId = v.VentaId,
                ApplicationUserId = v.ApplicationUserId,
                NombreUsuario = v.Usuario?.UserName ?? "Usuario desconocido",
                EmailUsuario = v.Usuario?.Email ?? "",
                Fecha = v.Fecha,
                Total = v.Total,
                Detalles = v.VentasDetalles.Select(d => new VentaDetalleResponseDto
                {
                    DetalleId = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.ProductoNombre ?? "Producto no disponible",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Cantidad * d.PrecioUnitario
                }).ToList(),
                Pago = v.VentasDetalles.FirstOrDefault()?.Pago != null
                    ? new PagoInfoDto
                    {
                        PagoId = v.VentasDetalles.First().Pago.PagoId,
                        NombreTitular = v.VentasDetalles.First().Pago.NombreTitular,
                        NumeroTarjetaEnmascarado = EnmascararTarjeta(v.VentasDetalles.First().Pago.NumeroTarjeta),
                        Direccion = v.VentasDetalles.First().Pago.Direccion
                    }
                    : new PagoInfoDto()
            }).ToList();

            var totalIngresos = await query.SumAsync(v => v.Total);

            return Ok(new
            {
                ventas = ventasDto,
                paginaActual = pagina,
                totalPaginas = totalPages,
                totalVentas = totalVentas,
                totalIngresos = totalIngresos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las ventas");
            return StatusCode(500, "Error al obtener las ventas");
        }
    }

    /// <summary>
    /// Obtiene estadísticas de ventas (Solo Admin)
    /// </summary>
    [HttpGet("estadisticas")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetEstadisticas(
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null)
    {
        try
        {
            var query = _context.Ventas.AsQueryable();

            if (fechaDesde.HasValue)
                query = query.Where(v => v.Fecha >= fechaDesde.Value);
            
            if (fechaHasta.HasValue)
                query = query.Where(v => v.Fecha <= fechaHasta.Value);

            var totalVentas = await query.CountAsync();
            var totalIngresos = await query.SumAsync(v => (decimal?)v.Total) ?? 0;
            var promedioVenta = totalVentas > 0 ? totalIngresos / totalVentas : 0;

            var ventasPorMes = await query
                .GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
                .Select(g => new
                {
                    Año = g.Key.Year,
                    Mes = g.Key.Month,
                    TotalVentas = g.Count(),
                    TotalIngresos = g.Sum(v => v.Total)
                })
                .OrderByDescending(x => x.Año)
                .ThenByDescending(x => x.Mes)
                .Take(12)
                .ToListAsync();

            // Obtener detalles de ventas para calcular productos más vendidos
            var detallesQuery = _context.Ventas
                .SelectMany(v => v.VentasDetalles)
                .Include(d => d.Producto)
                .AsQueryable();

            if (fechaDesde.HasValue)
                detallesQuery = detallesQuery.Where(d => d.Venta.Fecha >= fechaDesde.Value);
            
            if (fechaHasta.HasValue)
                detallesQuery = detallesQuery.Where(d => d.Venta.Fecha <= fechaHasta.Value);

            var productosMasVendidos = await detallesQuery
                .GroupBy(d => new { d.ProductoId, d.Producto!.ProductoNombre })
                .Select(g => new
                {
                    ProductoId = g.Key.ProductoId,
                    ProductoNombre = g.Key.ProductoNombre,
                    CantidadVendida = g.Sum(d => d.Cantidad),
                    TotalIngresos = g.Sum(d => d.Cantidad * d.PrecioUnitario)
                })
                .OrderByDescending(x => x.CantidadVendida)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                resumen = new
                {
                    totalVentas = totalVentas,
                    totalIngresos = totalIngresos,
                    promedioVenta = promedioVenta
                },
                ventasPorMes = ventasPorMes,
                productosMasVendidos = productosMasVendidos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de ventas");
            return StatusCode(500, "Error al obtener estadísticas");
        }
    }

    [HttpGet("{ventaId}")]
    public async Task<ActionResult<VentaResponseDto>> GetVenta(int ventaId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Usuario no autenticado");

            var venta = await _context.Ventas
                .Where(v => v.VentaId == ventaId && v.ApplicationUserId == userId)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Pago)
                .FirstOrDefaultAsync();

            if (venta == null)
                return NotFound($"Venta con ID {ventaId} no encontrada");

            var ventaDto = new VentaResponseDto
            {
                VentaId = venta.VentaId,
                ApplicationUserId = venta.ApplicationUserId,
                Fecha = venta.Fecha,
                Total = venta.Total,
                Detalles = venta.VentasDetalles.Select(d => new VentaDetalleResponseDto
                {
                    DetalleId = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.ProductoNombre ?? "Producto no disponible",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Cantidad * d.PrecioUnitario
                }).ToList(),
                Pago = venta.VentasDetalles.FirstOrDefault()?.Pago != null
                    ? new PagoInfoDto
                    {
                        PagoId = venta.VentasDetalles.First().Pago.PagoId,
                        NombreTitular = venta.VentasDetalles.First().Pago.NombreTitular,
                        NumeroTarjetaEnmascarado = EnmascararTarjeta(venta.VentasDetalles.First().Pago.NumeroTarjeta),
                        Direccion = venta.VentasDetalles.First().Pago.Direccion
                    }
                    : new PagoInfoDto()
            };

            return Ok(ventaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la venta");
            return StatusCode(500, "Error al obtener la venta");
        }
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<VentaResponseDto>> ProcessCheckout([FromBody] CreateVentaDto createVentaDto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Usuario no autenticado");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var carritoItems = await _context.CarritoItems
                .Where(c => c.ApplicationUserId == userId)
                .Include(c => c.Producto)
                .ToListAsync();

            if (!carritoItems.Any())
                return BadRequest("El carrito está vacío");

            foreach (var item in carritoItems)
            {
                var producto = await _context.Producto.FindAsync(item.ProductoId);
                if (producto == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Producto con ID {item.ProductoId} no encontrado");
                }

                if (producto.ProductoCantidad < item.Cantidad)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Stock insuficiente para {producto.ProductoNombre}. Disponible: {producto.ProductoCantidad}");
                }
            }

            var pago = new PagoModel
            {
                NombreTitular = createVentaDto.Pago.NombreTitular,
                NumeroTarjeta = createVentaDto.Pago.NumeroTarjeta,
                FechaExpiracion = createVentaDto.Pago.FechaExpiracion,
                CVV = createVentaDto.Pago.CVV,
                Direccion = createVentaDto.Pago.Direccion
            };

            _context.Pago.Add(pago);
            await _context.SaveChangesAsync();

            var venta = new Ventas
            {
                ApplicationUserId = userId,
                Fecha = DateTime.Now,
                Total = carritoItems.Sum(i => i.Cantidad * i.Producto.ProductoMonto),
                VentasDetalles = new List<VentasDetalles>()
            };

            foreach (var item in carritoItems)
            {
                var producto = await _context.Producto.FindAsync(item.ProductoId);
                if (producto != null)
                {
                    producto.ProductoCantidad -= item.Cantidad;

                    var detalle = new VentasDetalles
                    {
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Producto.ProductoMonto,
                        PagoId = pago.PagoId,
                        Venta = venta
                    };

                    venta.VentasDetalles.Add(detalle);
                }
            }

            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();

            _context.CarritoItems.RemoveRange(carritoItems);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var ventaCreada = await _context.Ventas
                .Where(v => v.VentaId == venta.VentaId)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Producto)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(d => d.Pago)
                .FirstOrDefaultAsync();

            var ventaResponse = new VentaResponseDto
            {
                VentaId = ventaCreada!.VentaId,
                ApplicationUserId = ventaCreada.ApplicationUserId,
                Fecha = ventaCreada.Fecha,
                Total = ventaCreada.Total,
                Detalles = ventaCreada.VentasDetalles.Select(d => new VentaDetalleResponseDto
                {
                    DetalleId = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto?.ProductoNombre ?? "Producto no disponible",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Cantidad * d.PrecioUnitario
                }).ToList(),
                Pago = new PagoInfoDto
                {
                    PagoId = pago.PagoId,
                    NombreTitular = pago.NombreTitular,
                    NumeroTarjetaEnmascarado = EnmascararTarjeta(pago.NumeroTarjeta),
                    Direccion = pago.Direccion
                }
            };

            return CreatedAtAction(nameof(GetVenta), new { ventaId = venta.VentaId }, ventaResponse);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al procesar la compra");
            return StatusCode(500, "Error al procesar la compra");
        }
    }
}
