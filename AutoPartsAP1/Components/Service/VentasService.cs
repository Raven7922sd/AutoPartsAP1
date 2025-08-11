using AutoPartsAP1.Components.Extensions;
using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Components.Models.Paginacion;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
            .ThenInclude(v => v.Pago)
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
            .Include(v => v.VentasDetalles)
                .ThenInclude(v => v.Pago)
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
        // Verifica si la lista de detalles es nula para evitar errores.
        if (ventas.VentasDetalles == null)
        {
            ventas.VentasDetalles = new List<VentasDetalles>();
        }

        using var context = await DbFactory.CreateDbContextAsync();

        // Inicia una transacción para garantizar la atomicidad de las operaciones.
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. Cargar la venta existente de la base de datos, incluyendo los detalles y pagos.
            var ventaExistente = await context.Ventas
                .Include(v => v.VentasDetalles)
                .ThenInclude(d => d.Pago)
                .FirstOrDefaultAsync(v => v.VentaId == ventas.VentaId);

            // Si la venta no existe, la modificación no es posible.
            if (ventaExistente == null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // 2. Sincronizar el inventario para productos eliminados o modificados.
            var detallesOriginales = ventaExistente.VentasDetalles.ToDictionary(d => d.ProductoId, d => d);
            var detallesActuales = ventas.VentasDetalles.ToDictionary(d => d.ProductoId, d => d);

            // a) Restaurar stock para productos eliminados de la venta.
            foreach (var original in detallesOriginales.Values)
            {
                if (!detallesActuales.ContainsKey(original.ProductoId))
                {
                    var producto = await context.Producto.FindAsync(original.ProductoId);
                    if (producto != null)
                    {
                        producto.ProductoCantidad += original.Cantidad;
                    }
                }
            }

            // b) Ajustar stock para productos agregados o modificados.
            foreach (var actual in detallesActuales.Values)
            {
                if (detallesOriginales.TryGetValue(actual.ProductoId, out var original))
                {
                    // Producto modificado.
                    int diferencia = (int)(actual.Cantidad - original.Cantidad);
                    var producto = await context.Producto.FindAsync(actual.ProductoId);
                    if (producto != null)
                    {
                        if (producto.ProductoCantidad < diferencia)
                        {
                            await transaction.RollbackAsync();
                            return false; // Stock insuficiente.
                        }
                        producto.ProductoCantidad -= diferencia;
                    }
                }
                else
                {
                    // Producto nuevo.
                    var producto = await context.Producto.FindAsync(actual.ProductoId);
                    if (producto != null)
                    {
                        if (producto.ProductoCantidad < actual.Cantidad)
                        {
                            await transaction.RollbackAsync();
                            return false; // Stock insuficiente.
                        }
                        producto.ProductoCantidad -= actual.Cantidad;
                    }
                }
            }

            // 3. Sincronizar los detalles de la venta en la base de datos.
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
                        context.Entry(detalleExistente).CurrentValues.SetValues(detalleActualizado);
                        if (detalleActualizado.Pago != null)
                        {
                            if (detalleExistente.Pago != null)
                            {
                                context.Entry(detalleExistente.Pago).CurrentValues.SetValues(detalleActualizado.Pago);
                            }
                            else
                            {
                                detalleExistente.Pago = detalleActualizado.Pago;
                            }
                        }
                    }
                }
                else
                {
                    ventaExistente.VentasDetalles.Add(detalleActualizado);
                }
            }

            // 4. Actualizar las propiedades de la venta principal (Ventas).
            context.Entry(ventaExistente).CurrentValues.SetValues(ventas);

            // 5. Guardar todos los cambios y confirmar la transacción.
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            // Loggear la excepción para depuración.
            return false;
        }
    }
    public async Task<Ventas?> ObtenerVentaConDetallesYPago(int id)
    {
        using var context = await DbFactory.CreateDbContextAsync();

        try
        {
            // La consulta original solo cargaba los detalles y el pago.
            // Ahora, también incluimos el Usuario de la venta.
            var venta = await context.Ventas
                .Include(v => v.VentasDetalles)
                    .ThenInclude(vd => vd.Producto)
                .Include(v => v.VentasDetalles)
                    .ThenInclude(vd => vd.Pago)
                // ---- ¡NUEVA LÍNEA AÑADIDA AQUÍ! ----
                // Carga el objeto Usuario asociado a la venta.
                .FirstOrDefaultAsync(v => v.VentaId == id);

            return venta;
        }
        catch (Exception ex)
        {
            // Manejo de errores
            Console.WriteLine($"Error al obtener la venta con detalles y pago: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> AgregarProductoDetalle(Ventas venta, int selectedProductoId, int cantidadDetalle)
    {
        // Verificación inicial de parámetros.
        if (selectedProductoId == 0 || cantidadDetalle <= 0)
        {
            return false;
        }

        await using var context = await DbFactory.CreateDbContextAsync();

        // Obtener el producto sin rastreo para no afectar el contexto.
        var productoSeleccionado = await context.Producto.AsNoTracking().FirstOrDefaultAsync(p => p.ProductoId == selectedProductoId);
        if (productoSeleccionado == null)
        {
            return false;
        }

        // Calcular la cantidad total del producto en la lista de detalles actual.
        var currentQuantityInDetails = venta.VentasDetalles
                                            .Where(d => d.ProductoId == selectedProductoId)
                                            .Sum(d => d.Cantidad);

        // Calcular la cantidad total proyectada.
        var projectedTotalQuantity = currentQuantityInDetails + cantidadDetalle;

        // Verificar si la cantidad total excede el stock disponible.
        if (productoSeleccionado.ProductoCantidad < projectedTotalQuantity)
        {
            return false;
        }

        // Buscar si el producto ya existe en la lista de detalles.
        var detalleExistente = venta.VentasDetalles.FirstOrDefault(d => d.ProductoId == selectedProductoId);
        if (detalleExistente != null)
        {
            // Si existe, solo se actualiza la cantidad.
            detalleExistente.Cantidad += cantidadDetalle;
        }
        else
        {
            // Si no existe, se crea un nuevo detalle.
            var nuevoDetalle = new VentasDetalles
            {
                ProductoId = selectedProductoId,
                Producto = productoSeleccionado,
                Cantidad = cantidadDetalle,
                // ¡IMPORTANTE! Asignar la venta al nuevo detalle para evitar el error de referencia nula.
                Venta = venta
            };
            venta.VentasDetalles.Add(nuevoDetalle);
        }

        return true;
    }
}