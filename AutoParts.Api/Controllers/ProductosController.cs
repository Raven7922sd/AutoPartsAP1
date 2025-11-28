using AutoParts.Shared.Data;
using AutoParts.Shared.DTOs;
using AutoParts.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly ProductoService _productoService;
    private readonly ILogger<ProductosController> _logger;

    public ProductosController(ProductoService productoService, ILogger<ProductosController> logger)
    {
        _productoService = productoService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductoDto>>> GetProductos()
    {
        try
        {
            var productos = await _productoService.Listar(p => true);
            var productosDto = productos.Select(p => new ProductoDto
            {
                ProductoId = p.ProductoId,
                ProductoNombre = p.ProductoNombre,
                ProductoMonto = p.ProductoMonto,
                ProductoCantidad = p.ProductoCantidad,
                ProductoDescripcion = p.ProductoDescripcion,
                ProductoImagenBase64 = p.ProductoImagen != null ? Convert.ToBase64String(p.ProductoImagen) : null,
                ProductoImagenUrl = p.ProductoImagenUrl,
                Categoria = p.Categoria,
                Fecha = p.Fecha
            }).ToList();

            return Ok(productosDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos");
            return StatusCode(500, "Error al obtener productos");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductoDto>> GetProducto(int id)
    {
        try
        {
            var producto = await _productoService.Buscar(id);
            if (producto == null)
                return NotFound($"Producto con ID {id} no encontrado");

            var productoDto = new ProductoDto
            {
                ProductoId = producto.ProductoId,
                ProductoNombre = producto.ProductoNombre,
                ProductoMonto = producto.ProductoMonto,
                ProductoCantidad = producto.ProductoCantidad,
                ProductoDescripcion = producto.ProductoDescripcion,
                ProductoImagenBase64 = producto.ProductoImagen != null ? Convert.ToBase64String(producto.ProductoImagen) : null,
                ProductoImagenUrl = producto.ProductoImagenUrl,
                Categoria = producto.Categoria,
                Fecha = producto.Fecha
            };

            return Ok(productoDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener producto");
            return StatusCode(500, "Error al obtener producto");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Productos>> CreateProducto([FromBody] Productos producto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var guardado = await _productoService.Guardar(producto);
            if (!guardado)
                return BadRequest("No se pudo guardar el producto");

            return CreatedAtAction(nameof(GetProducto), new { id = producto.ProductoId }, producto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear producto");
            return StatusCode(500, "Error al crear producto");
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> UpdateProducto(int id, [FromBody] Productos producto)
    {
        try
        {
            if (id != producto.ProductoId)
                return BadRequest("ID de producto no coincide");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existe = await _productoService.ExisteId(id);
            if (!existe)
                return NotFound($"Producto con ID {id} no encontrado");

            var actualizado = await _productoService.Guardar(producto);
            if (!actualizado)
                return BadRequest("No se pudo actualizar el producto");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar producto");
            return StatusCode(500, "Error al actualizar producto");
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteProducto(int id)
    {
        try
        {
            var existe = await _productoService.ExisteId(id);
            if (!existe)
                return NotFound($"Producto con ID {id} no encontrado");

            var eliminado = await _productoService.Eliminar(id);
            if (!eliminado)
                return BadRequest("No se pudo eliminar el producto. Puede estar en uso.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar producto");
            return StatusCode(500, "Error al eliminar producto");
        }
    }
}
