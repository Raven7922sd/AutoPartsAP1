using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsAP1.Components.Service;

public class ComprasService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
  
    public async Task<List<VentaDto>> ObtenerVentasPorUsuario(string userId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();

        return await contexto.Ventas
            .Where(v => v.ApplicationUserId == userId)
            .OrderByDescending(v => v.Fecha)
            .Select(v => new VentaDto
            {
                VentaId = v.VentaId,
                Fecha = v.Fecha,
                Total = v.VentasDetalles.Sum(d => d.Producto.ProductoMonto * d.Cantidad),
                Detalles = v.VentasDetalles.Select(d => new VentaDetalleDto
                {
                    ProductoId = d.ProductoId,
                    ProductoNombre = d.Producto.ProductoNombre,
                    Precio = d.Producto.ProductoMonto,
                    Cantidad = d.Cantidad
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<FacturaDto?> ObtenerFacturaPorIdAsync(int ventaId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();

        var venta = await contexto.Ventas
            .Include(v => v.VentasDetalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.VentaId == ventaId);

        if (venta == null)
            return null;

        return new FacturaDto
        {
            VentaId = venta.VentaId,
            Fecha = venta.Fecha,
            UsuarioEmail = venta.Usuario?.Email ?? string.Empty,
            Total = venta.VentasDetalles.Sum(d => d.Cantidad * d.PrecioUnitario),
            Detalles = venta.VentasDetalles.Select(d => new FacturaDetalleDto
            {
                ProductoNombre = d.Producto?.ProductoNombre ?? string.Empty,
                Categoria = d.Producto?.Categoria ?? string.Empty,
                PrecioUnitario = d.PrecioUnitario,
                Cantidad = d.Cantidad,
                Subtotal = d.Cantidad * d.PrecioUnitario
            }).ToList()
        };
    }

}
