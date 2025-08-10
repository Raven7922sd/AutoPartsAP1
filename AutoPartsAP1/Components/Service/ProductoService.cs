using AutoPartsAP1.Components.Extensions;
using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Components.Models.Paginacion;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutoPartsAP1.Components.Services;

public class ProductoService(IDbContextFactory<ApplicationDbContext>DbFactory)
{
    public async Task<bool> Guardar(Productos producto)
    {
        if (!await ExisteId(producto.ProductoId))
        { return (await Insertar(producto)); }

        else { return await Modificar(producto); }
    }

    public async Task<bool> ExisteId(int productoId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Producto.AnyAsync(a => a.ProductoId == productoId);
    }

    public async Task<bool> ExisteNombre(string Nombre)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Producto.AnyAsync(n => n.ProductoNombre.ToLower() == Nombre.ToLower());
    }

    public async Task<bool> Insertar(Productos producto)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        context.Producto.Add(producto);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> Modificar(Productos producto)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        context.Producto.Update(producto);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> Eliminar(int productoId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Producto.AsNoTracking().Where(a => a.ProductoId == productoId).ExecuteDeleteAsync() > 0;
    }

    public async Task<Productos?> Buscar(int productoId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Producto.FirstOrDefaultAsync(a => a.ProductoId == productoId);
    }

  
    public async Task<List<Productos>> Listar(Expression<Func<Productos, bool>> criterio)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Producto.Where(criterio).AsNoTracking().ToListAsync();
    }
    public async Task<PaginacionResultado<Productos>> BuscarFiltradosAsync(
        string filtroCampo,
        string valorFiltro,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int pagina,
        int tamanioPagina)
    {
        Expression<Func<Productos, bool>> filtro = p => true;

        if (!string.IsNullOrWhiteSpace(valorFiltro))
        {
            var valor = valorFiltro.ToLower();

            if (filtroCampo == "ProductoId" && int.TryParse(valorFiltro, out var productoId))
                filtro = filtro.AndAlso(p => p.ProductoId == productoId);
            else if (filtroCampo == "Nombre")
                filtro = filtro.AndAlso(p => p.ProductoNombre.ToLower().Contains(valor));
            else if (filtroCampo == "Descripción")
                filtro = filtro.AndAlso(p => p.ProductoDescripcion.ToLower().Contains(valor));
            else if (filtroCampo == "Monto" && double.TryParse(valorFiltro, out var monto))
                filtro = filtro.AndAlso(p => p.ProductoMonto == monto);
        }

        if (filtroCampo is "Uso General" or "Motocicletas" or "Autos o Vehículos Ligeros" or "Vehículos Pesados")
        {
            filtro = filtro.AndAlso(p => p.Categoria.ToLower() == filtroCampo.ToLower());
        }

        if (fechaDesde.HasValue)
            filtro = filtro.AndAlso(p => p.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue)
            filtro = filtro.AndAlso(p => p.Fecha <= fechaHasta.Value);

        await using var context = await DbFactory.CreateDbContextAsync();

        var totalRegistros = await context.Producto.CountAsync(filtro); // 👈 plural
        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)tamanioPagina);

        var productos = await context.Producto // 👈 plural
            .Where(filtro)
            .OrderBy(p => p.Fecha)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .AsNoTracking()
            .ToListAsync();

        return new PaginacionResultado<Productos>
        {
            Items = productos,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas
        };
    }



    public async Task<bool> RestarExistenciaProductoAsync(int productoId, int cantidad)
    {
        await using var context = await DbFactory.CreateDbContextAsync();

        var producto = await context.Producto.FindAsync(productoId);
        if (producto == null || producto.ProductoCantidad < cantidad)
        {
            return false;
        }

        producto.ProductoCantidad -= cantidad;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestaurarExistenciaProductoAsync(int productoId, int cantidad)
    {
        await using var context = await DbFactory.CreateDbContextAsync();

        var producto = await context.Producto.FindAsync(productoId);
        if (producto == null)
        {
            return false;
        }

        producto.ProductoCantidad += cantidad;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AgregarProductoDetalle(Ventas venta, int selectedProductoId, int cantidadDetalle)
    {
        if (selectedProductoId == 0 || cantidadDetalle <= 0)
        {
            return false;
        }

        await using var context = await DbFactory.CreateDbContextAsync();


        var productoSeleccionado = await context.Producto.AsNoTracking().FirstOrDefaultAsync(p => p.ProductoId == selectedProductoId);
        if (productoSeleccionado == null)
        {
            return false;
        }

        var currentQuantityInDetails = venta.VentasDetalles
                                            .Where(d => d.ProductoId == selectedProductoId)
                                            .Sum(d => d.Cantidad);

        var projectedTotalQuantity = currentQuantityInDetails + cantidadDetalle;

        if (productoSeleccionado.ProductoCantidad < projectedTotalQuantity)
        {
            return false;
        }

        var detalleExistente = venta.VentasDetalles.FirstOrDefault(d => d.ProductoId == selectedProductoId);
        if (detalleExistente != null)
        {
            detalleExistente.Cantidad += cantidadDetalle;
        }
        else
        {
            var nuevoDetalle = new VentasDetalles
            {
                ProductoId = selectedProductoId,
                Producto = productoSeleccionado,
                Cantidad = cantidadDetalle, 
            };
            venta.VentasDetalles.Add(nuevoDetalle);
        }

        return true;
    }
}