using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsAP1.Components.Service;

public class ComprasService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
  
    public async Task<List<Ventas>> ObtenerVentasPorUsuario(string userId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();

        return await contexto.Ventas
            .Include(v => v.VentasDetalles)
            .ThenInclude(d => d.Producto)
            .Include(v => v.VentasDetalles)
            .ThenInclude(d => d.Pago)
            .Where(v => v.ApplicationUserId == userId)
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();
    }

    public async Task<Ventas?> ObtenerFacturaPorIdAsync(int ventaId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();

        return await contexto.Ventas
            .Include(v => v.VentasDetalles)
            .ThenInclude(d => d.Producto)
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.VentaId == ventaId);
    }
}
