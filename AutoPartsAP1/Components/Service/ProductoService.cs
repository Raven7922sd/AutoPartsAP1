using AutoPartsAP1.Components.Models;
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
    public async Task<List<Productos>> BuscarFiltradosAsync(
    string filtroCampo,
    string valorFiltro,
    DateTime? fechaDesde,
    DateTime? fechaHasta)
    {
        await using var context = await DbFactory.CreateDbContextAsync();

        IQueryable<Productos> query = context.Producto.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(valorFiltro))
        {
            var valor = valorFiltro.ToLower();

            if (filtroCampo == "ProductoId" && int.TryParse(valorFiltro, out var productoId))
            {
                query = query.Where(a => a.ProductoId == productoId);
            }
            else if (filtroCampo == "Nombre")
            {
                query = query.Where(a => a.ProductoNombre.ToLower().Contains(valor));
            }
            else if (filtroCampo == "Monto" && double.TryParse(valorFiltro, out var monto))
            {
                query = query.Where(m => m.ProductoMonto == monto);
            }
            else if (filtroCampo == "Cantidad" && double.TryParse(valorFiltro, out var cantidad))
            {
                query = query.Where(m => m.ProductoCantidad == cantidad);
            }
        }

        if (fechaDesde.HasValue)
            query = query.Where(f => f.Fecha >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(f => f.Fecha <= fechaHasta.Value);

        return await query.OrderBy(f => f.Fecha).ToListAsync();
    }
}