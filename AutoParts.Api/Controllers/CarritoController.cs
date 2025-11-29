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
        public class CarritoController : ControllerBase
        {
            private readonly ApplicationDbContext _context;
            private readonly ILogger<CarritoController> _logger;

            public CarritoController(ApplicationDbContext context, ILogger<CarritoController> logger)
            {
                _context = context;
                _logger = logger;
            }

            private string GetUserId()
            {
                return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            }

            [HttpGet]
            public async Task<ActionResult<List<CarritoDto>>> GetCarrito()
            {
                try
                {
                    var userId = GetUserId();
                    if (string.IsNullOrEmpty(userId))
                        return Unauthorized("Usuario no autenticado");

                    var items = await _context.CarritoItems
                        .Where(c => c.ApplicationUserId == userId)
                        .Include(c => c.Producto)
                        .Select(c => new CarritoDto
                        {
                            CarritoId = c.CarritoId,
                            ApplicationUserId = c.ApplicationUserId,
                            ProductoId = c.ProductoId,
                            Producto = new ProductoDto
                            {
                                ProductoId = c.Producto.ProductoId,
                                ProductoNombre = c.Producto.ProductoNombre,
                                ProductoMonto = c.Producto.ProductoMonto,
                                ProductoCantidad = c.Producto.ProductoCantidad,
                                ProductoDescripcion = c.Producto.ProductoDescripcion,
                                ProductoImagenUrl = c.Producto.ProductoImagenUrl,
                                Categoria = c.Producto.Categoria,
                                Fecha = c.Producto.Fecha
                            },
                            Cantidad = c.Cantidad
                        })
                        .ToListAsync();

                    return Ok(items);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener el carrito");
                    return StatusCode(500, "Error al obtener el carrito");
                }
            }

            [HttpGet("total")]
            public async Task<ActionResult<object>> GetTotal()
            {
                try
                {
                    var userId = GetUserId();
                    if (string.IsNullOrEmpty(userId))
                        return Unauthorized("Usuario no autenticado");

                    var items = await _context.CarritoItems
                        .Where(c => c.ApplicationUserId == userId)
                        .Include(c => c.Producto)
                        .ToListAsync();

                    var totalItems = items.Sum(i => i.Cantidad);
                    var totalPrice = items.Sum(i => i.Cantidad * i.Producto.ProductoMonto);

                    return Ok(new
                    {
                        TotalItems = totalItems,
                        TotalPrice = totalPrice
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener el total del carrito");
                    return StatusCode(500, "Error al obtener el total del carrito");
                }
            }

            [HttpPost]
            public async Task<ActionResult<CarritoDto>> AddItem([FromBody] AddCarritoDto addCarritoDto)
            {
                try
                {
                    var userId = GetUserId();
                    if (string.IsNullOrEmpty(userId))
                        return Unauthorized("Usuario no autenticado");

                    if (addCarritoDto.Cantidad <= 0)
                        return BadRequest("La cantidad debe ser mayor a 0");

                    var producto = await _context.Producto.FindAsync(addCarritoDto.ProductoId);
                    if (producto == null)
                        return NotFound($"Producto con ID {addCarritoDto.ProductoId} no encontrado");

                    var itemExistente = await _context.CarritoItems
                        .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductoId == addCarritoDto.ProductoId);

                    if (itemExistente != null)
                    {
                        var nuevaCantidad = itemExistente.Cantidad + addCarritoDto.Cantidad;
                        if (nuevaCantidad > producto.ProductoCantidad)
                            return BadRequest($"Stock insuficiente. Disponible: {producto.ProductoCantidad}");

                        itemExistente.Cantidad = nuevaCantidad;
                        await _context.SaveChangesAsync();

                        var itemActualizado = await _context.CarritoItems
                            .Include(c => c.Producto)
                            .FirstOrDefaultAsync(c => c.CarritoId == itemExistente.CarritoId);

                        return Ok(MapToDto(itemActualizado!));
                    }
                    else
                    {
                        if (addCarritoDto.Cantidad > producto.ProductoCantidad)
                            return BadRequest($"Stock insuficiente. Disponible: {producto.ProductoCantidad}");

                        var nuevoItem = new Carrito
                        {
                            ApplicationUserId = userId,
                            ProductoId = addCarritoDto.ProductoId,
                            Cantidad = addCarritoDto.Cantidad
                        };

                        _context.CarritoItems.Add(nuevoItem);
                        await _context.SaveChangesAsync();

                        var itemCreado = await _context.CarritoItems
                            .Include(c => c.Producto)
                            .FirstOrDefaultAsync(c => c.CarritoId == nuevoItem.CarritoId);

                        return CreatedAtAction(nameof(GetCarrito), MapToDto(itemCreado!));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al agregar item al carrito");
                    return StatusCode(500, "Error al agregar item al carrito");
                }
            }

            [HttpPut("{carritoId}")]
            public async Task<ActionResult> UpdateItem(int carritoId, [FromBody] UpdateCarritoDto updateCarritoDto)
            {
                try
                {
                    var userId = GetUserId();
                    if (string.IsNullOrEmpty(userId))
                        return Unauthorized("Usuario no autenticado");

                    var item = await _context.CarritoItems
                        .Include(c => c.Producto)
                        .FirstOrDefaultAsync(c => c.CarritoId == carritoId && c.ApplicationUserId == userId);

                    if (item == null)
                        return NotFound($"Item del carrito con ID {carritoId} no encontrado");

                    if (updateCarritoDto.Cantidad <= 0)
                        return BadRequest("La cantidad debe ser mayor a 0");

                    if (updateCarritoDto.Cantidad > item.Producto.ProductoCantidad)
                        return BadRequest($"Stock insuficiente. Disponible: {item.Producto.ProductoCantidad}");

                    item.Cantidad = updateCarritoDto.Cantidad;
                    await _context.SaveChangesAsync();

                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al actualizar item del carrito");
                    return StatusCode(500, "Error al actualizar item del carrito");
                }
            }

            [HttpDelete("{carritoId}")]
            public async Task<ActionResult> DeleteItem(int carritoId)
            {
                try
                {
                    var userId = GetUserId();
                    if (string.IsNullOrEmpty(userId))
                        return Unauthorized("Usuario no autenticado");

                    var item = await _context.CarritoItems
                        .FirstOrDefaultAsync(c => c.CarritoId == carritoId && c.ApplicationUserId == userId);

                    if (item == null)
                        return NotFound($"Item del carrito con ID {carritoId} no encontrado");

                    _context.CarritoItems.Remove(item);
                    await _context.SaveChangesAsync();

                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar item del carrito");
                    return StatusCode(500, "Error al eliminar item del carrito");
                }
            }

            [HttpDelete("clear")]
            public async Task<ActionResult> ClearCarrito()
            {
                try
                {
                    var userId = GetUserId();
                    if (string.IsNullOrEmpty(userId))
                        return Unauthorized("Usuario no autenticado");

                    var items = await _context.CarritoItems
                        .Where(c => c.ApplicationUserId == userId)
                        .ToListAsync();

                    if (items.Any())
                    {
                        _context.CarritoItems.RemoveRange(items);
                        await _context.SaveChangesAsync();
                    }

                    return NoContent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al vaciar el carrito");
                    return StatusCode(500, "Error al vaciar el carrito");
                }
            }

            private static CarritoDto MapToDto(Carrito carrito)
            {
                return new CarritoDto
                {
                    CarritoId = carrito.CarritoId,
                    ApplicationUserId = carrito.ApplicationUserId,
                    ProductoId = carrito.ProductoId,
                    Producto = new ProductoDto
                    {
                        ProductoId = carrito.Producto.ProductoId,
                        ProductoNombre = carrito.Producto.ProductoNombre,
                        ProductoMonto = carrito.Producto.ProductoMonto,
                        ProductoCantidad = carrito.Producto.ProductoCantidad,
                        ProductoDescripcion = carrito.Producto.ProductoDescripcion,
                        ProductoImagenUrl = carrito.Producto.ProductoImagenUrl,
                        Categoria = carrito.Producto.Categoria,
                        Fecha = carrito.Producto.Fecha
                    },
                    Cantidad = carrito.Cantidad
                };
            }
        }
