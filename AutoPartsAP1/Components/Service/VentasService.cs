using AutoPartsAP1.Components.Extensions;
using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Components.Models.Paginacion;
using AutoPartsAP1.Components.Services;
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
        await using var context = await DbFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            context.Ventas.Add(ventas);

            foreach (var detalle in ventas.VentasDetalles)
            {
                var producto = await context.Producto.FindAsync(detalle.ProductoId);
                if (producto != null)
                {

                    if (producto.ProductoCantidad < detalle.Cantidad)
                    {

                        await transaction.RollbackAsync();
                        return false;
                    }

                    producto.ProductoCantidad -= detalle.Cantidad;
                }
                else
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }

            var cambios = await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return cambios > 0;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return false;
        }
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
        else if (filtroCampo == "Usuario")
            filtro = filtro.AndAlso(t => t.Usuario != null &&
                                         t.Usuario.Email.ToLower().Contains(valorFiltro.ToLower()));
        else if (filtroCampo == "Producto")
            filtro = filtro.AndAlso(t => t.VentasDetalles
                                         .Any(d => d.Producto.ProductoNombre.ToLower().Contains(valorFiltro.ToLower())));
        else if (filtroCampo == "Monto" && double.TryParse(valorFiltro, out var monto))
            filtro = filtro.AndAlso(m => m.VentasDetalles.FirstOrDefault().Venta.Total == monto);
        else if (filtroCampo == "Dirección")
            filtro = filtro.AndAlso(t => t.VentasDetalles
                                         .Any(d => d.Pago.Direccion.ToLower().Contains(valorFiltro.ToLower())));
        if (filtroCampo is "Uso General" or "Motocicletas" or "Autos o Vehículos Ligeros" or "Vehículos Pesados")
        {
            filtro = filtro.AndAlso(t => t.VentasDetalles
                                         .Any(d => d.Producto.Categoria.ToLower() == filtroCampo.ToLower()));
        }

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
            .Include(v=>v.VentasDetalles)
                .ThenInclude(v=>v.Pago)
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
        if (ventas.VentasDetalles == null)
            ventas.VentasDetalles = new List<VentasDetalles>();

        using var context = await DbFactory.CreateDbContextAsync();

        var ventaExistente = await context.Ventas
            .Include(v => v.VentasDetalles)
            .ThenInclude(d => d.Pago)
            .FirstOrDefaultAsync(v => v.VentaId == ventas.VentaId);

        if (ventaExistente == null)
            return false;


        ventaExistente.Total = ventas.Total;
        ventaExistente.Fecha = ventas.Fecha;

        var detallesAEliminar = ventaExistente.VentasDetalles
            .Where(detalleDb => !ventas.VentasDetalles.Any(d => d.Id == detalleDb.Id))
            .ToList();
        context.VentasDetalle.RemoveRange(detallesAEliminar);

        foreach (var detalleActualizado in ventas.VentasDetalles)
        {
            if (detalleActualizado.Id > 0)
            {
                var detalleExistente = ventaExistente.VentasDetalles
                    .FirstOrDefault(d => d.Id == detalleActualizado.Id);

                if (detalleExistente != null)
                {
                    detalleExistente.Cantidad = detalleActualizado.Cantidad;
                    detalleExistente.ProductoId = detalleActualizado.ProductoId;
                    detalleExistente.PrecioUnitario = detalleActualizado.PrecioUnitario;

                    if (detalleExistente.Pago != null && detalleActualizado.Pago != null)
                    {
                        detalleExistente.Pago.CVV = detalleActualizado.Pago.CVV;
                        detalleExistente.Pago.NumeroTarjeta = detalleActualizado.Pago.NumeroTarjeta;
                        detalleExistente.Pago.NombreTitular = detalleActualizado.Pago.NombreTitular;
                        detalleExistente.Pago.FechaExpiracion = detalleActualizado.Pago.FechaExpiracion;
                        detalleExistente.Pago.Direccion = detalleActualizado.Pago.Direccion;
                    }
                }
            }
            else
            {
    
                ventaExistente.VentasDetalles.Add(detalleActualizado);
            }
        }

        return await context.SaveChangesAsync() > 0;
    }

    public bool AgregarProductoDetalleMemoria(Ventas venta, int productoId, double cantidad, List<Productos> productosEnMemoria)
    {
        if (venta == null) return false;

        var producto = productosEnMemoria.FirstOrDefault(p => p.ProductoId == productoId);
        if (producto == null) return false;

        var detalleExistente = venta.VentasDetalles?.FirstOrDefault(d => d.ProductoId == productoId);
        var cantidadActualEnVenta = detalleExistente?.Cantidad ?? 0;

        if (producto.ProductoCantidad < (cantidadActualEnVenta + cantidad))
        {
            return false;
        }

        if (detalleExistente != null)
        {
            detalleExistente.Cantidad += cantidad;
        }
        else
        {
            var nuevoDetalle = new VentasDetalles
            {
                ProductoId = producto.ProductoId,
                Producto = producto,
                Cantidad = cantidad,
                Venta = venta,
                Pago = new PagoModel()
            };

            venta.VentasDetalles ??= new List<VentasDetalles>();
            venta.VentasDetalles.Add(nuevoDetalle);
        }

        venta.Total = venta.VentasDetalles.Sum(d => d.Cantidad * d.Producto.ProductoMonto);
        return true;
    }

}