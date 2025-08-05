using AutoPartsAP1.Components.Extensions;
using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Components.Models.Paginacion;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutoPartsAP1.Components.Service;

public class VentasService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
    public async Task<bool> Guardar(Ventas ventas)
    {
        if (!await ExisteId(ventas.VentaId))
            return await Insertar(ventas);
        else
            return await Modificar(ventas);
    }

    public async Task<bool> ExisteId(int VentaId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Ventas.AnyAsync(v => v.VentaId == VentaId);
    }

    public async Task<bool> Insertar(Ventas ventas)
    {
        using var context = await DbFactory.CreateDbContextAsync();
        context.Ventas.Add(ventas);

        foreach (var detalle in ventas.VentasDetalles)
        {
            var producto = await context.Producto.FindAsync(detalle.ProductoId);
            if (producto != null)
            {
                producto.ProductoCantidad -= detalle.Cantidad;
            }
        }

        return await context.SaveChangesAsync() > 0;
    }

    public async Task<Ventas?> BuscarVentas(int Ventaid)
    {
        await using var context = await DbFactory.CreateDbContextAsync();

        return await context.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.VentasDetalles)
            .ThenInclude(d => d.Producto)
            .Include(v => v.VentasDetalles)
            .ThenInclude(v=>v.Pago)
            .FirstOrDefaultAsync(v => v.VentaId == Ventaid);
    }

    public async Task<bool> EliminarVentas(int ventaId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var venta = await context.Ventas
                .Include(v => v.VentasDetalles)
                .FirstOrDefaultAsync(v => v.VentaId == ventaId);

            if (venta == null)
                return false;

            foreach (var detalle in venta.VentasDetalles)
            {
                var producto = await context.Producto.FindAsync(detalle.ProductoId);
                if (producto != null)
                {
                    producto.ProductoCantidad += detalle.Cantidad;
                }
            }

            context.VentasDetalle.RemoveRange(venta.VentasDetalles);
            context.Ventas.Remove(venta);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<List<Ventas>> ListarVentas(Expression<Func<Ventas, bool>> criterio)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Ventas
            .Where(criterio)
            .Include(v => v.Usuario)
            .Include(v => v.VentasDetalles)
                .ThenInclude(d => d.Producto)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Ventas>> ObtenerVentasPorUsuario(string userId)
    {
        await using var contexto = await DbFactory.CreateDbContextAsync();
        return await contexto.Ventas
            .Include(v => v.VentasDetalles)
            .ThenInclude(d => d.Producto)
            .Where(v => v.ApplicationUserId == userId)
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();
    }

    public async Task<PaginacionResultado<Ventas>> BuscarVentasAsync(
        string filtroCampo,
        string valorFiltro,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanioPagina)
    {
        Expression<Func<Ventas, bool>> filtro = t => true;

        if (filtroCampo == "VentaId" && int.TryParse(valorFiltro, out var ventaid))
            filtro = filtro.AndAlso(t => t.VentaId == ventaid);
        else if (filtroCampo == "UsuarioNombre")
            filtro = filtro.AndAlso(t => t.Usuario != null && t.Usuario.UserName.ToLower().Contains(valorFiltro.ToLower()));
        else if (filtroCampo == "ProductoNombre")
            filtro = filtro.AndAlso(t => t.VentasDetalles.Any(d => d.Producto.ProductoNombre.ToLower().Contains(valorFiltro.ToLower())));

        if (fechaDesde.HasValue)
            filtro = filtro.AndAlso(t => t.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue)
            filtro = filtro.AndAlso(t => t.Fecha <= fechaHasta.Value);

        await using var context = await DbFactory.CreateDbContextAsync();

        var totalRegistros = await context.Ventas.CountAsync(filtro);
        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanioPagina);

        var ventas = await context.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.VentasDetalles)
                .ThenInclude(d => d.Producto)
            .Where(filtro)
            .OrderBy(v => v.VentaId)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .AsNoTracking()
            .ToListAsync();

        return new PaginacionResultado<Ventas>
        {
            Items = ventas,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas
        };
    }
    public async Task<PagoModel?> GuardarPago(PagoModel pago)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        context.Pago.Add(pago);
        var guardado = await context.SaveChangesAsync() > 0;

        return guardado ? pago : null;
    }

    public async Task<bool> Modificar(Ventas ventas)
    {
        using var context = await DbFactory.CreateDbContextAsync();

        var ventaExistente = await context.Ventas
            .Include(v => v.VentasDetalles)
                .ThenInclude(d => d.Pago)
            .FirstOrDefaultAsync(v => v.VentaId == ventas.VentaId);

        if (ventaExistente == null)
            return false;

        context.Entry(ventaExistente).CurrentValues.SetValues(ventas);

        foreach (var detalle in ventas.VentasDetalles)
        {
            var detalleExistente = ventaExistente.VentasDetalles
                .FirstOrDefault(d => d.Id == detalle.Id);

            if (detalleExistente != null)
            {
                context.Entry(detalleExistente).CurrentValues.SetValues(detalle);

                if (detalle.Pago != null)
                {
                    if (detalleExistente.Pago != null)
                    {
                        context.Entry(detalleExistente.Pago).CurrentValues.SetValues(detalle.Pago);
                    }
                    else
                    {
                        detalleExistente.Pago = detalle.Pago;
                    }
                }
            }
            else
            {
                ventaExistente.VentasDetalles.Add(detalle);
            }
        }

        foreach (var detalleDb in ventaExistente.VentasDetalles.ToList())
        {
            if (!ventas.VentasDetalles.Any(d => d.Id == detalleDb.Id))
            {
                context.VentasDetalle.Remove(detalleDb);
            }
        }

        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> InsertarVentaServiciosAsync(Ventas venta, List<Servicios> servicios)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            context.Ventas.Add(venta);
            await context.SaveChangesAsync();

            foreach (var servicio in servicios)
            {
                var servicioDb = await context.Servicio.FirstOrDefaultAsync(s => s.ServicioId == servicio.ServicioId);
                if (servicioDb is not null)
                {
                    servicioDb.Solicitados += 1;
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
}