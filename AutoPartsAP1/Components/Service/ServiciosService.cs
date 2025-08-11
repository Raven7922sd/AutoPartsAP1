using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AutoPartsAP1.Components.Service;

public class ServiciosService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
    public async Task<bool> GuardarServicio(Servicios servicio)
    {
        if (!await ExisteServicioId(servicio.ServicioId))
        { return (await InsertarServicio(servicio)); }

        else { return await ModificarServicio(servicio); }
    }

    public async Task<bool> ExisteServicioId(int servicioId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Servicio.AnyAsync(a => a.ServicioId == servicioId);
    }

    public async Task<bool> InsertarServicio(Servicios servicio)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        context.Servicio.Add(servicio);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ModificarServicio(Servicios servicio)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        context.Servicio.Update(servicio);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> Eliminar(int servicioId)
    {
        var servicioAEliminar = await Buscar(servicioId);
        if (servicioAEliminar == null)
            return false;

        if (await ExistenCitasConServicio(servicioAEliminar.Nombre))
        {
            return false;
        }

        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Servicio.AsNoTracking().Where(a => a.ServicioId == servicioId).ExecuteDeleteAsync() > 0;
    }

    public async Task<bool> ExistenCitasConServicio(string nombreServicio)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Citas.AnyAsync(c => c.ServicioSolicitado == nombreServicio);
    }

    public async Task<Servicios?> Buscar(int servicioId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Servicio.FirstOrDefaultAsync(a => a.ServicioId == servicioId);
    }    
    
    public async Task<Servicios?> BuscarPorNombre(string ServicioNombre)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Servicio.FirstOrDefaultAsync(a => a.Nombre == ServicioNombre);
    }


    public async Task<List<Servicios>> Listar(Expression<Func<Servicios, bool>> criterio)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Servicio.Where(criterio).AsNoTracking().ToListAsync();
    }
    public async Task<List<Servicios>> BuscarFiltradosAsync(
    string filtroCampo,
    string valorFiltro,
    DateTime? fechaDesde,
    DateTime? fechaHasta)
    {
        await using var context = await DbFactory.CreateDbContextAsync();

        IQueryable<Servicios> query = context.Servicio.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(valorFiltro))
        {
            var valor = valorFiltro.ToLower();

            if (filtroCampo == "ServicioId" && int.TryParse(valorFiltro, out var ServicioId))
            {
                query = query.Where(a => a.ServicioId == ServicioId);
            }
            else if (filtroCampo == "Nombre")
            {
                query = query.Where(a => a.Nombre.ToLower().Contains(valor));
            }
            else if (filtroCampo == "Descripción")
            {
                query = query.Where(a => a.Descripcion.ToLower().Contains(valor));
            }
            else if (filtroCampo == "Monto" && double.TryParse(valorFiltro, out var monto))
            {
                query = query.Where(m => m.Precio == monto);
            }
        }

        if (fechaDesde.HasValue)
            query = query.Where(f => f.FechaServicio >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(f => f.FechaServicio <= fechaHasta.Value);

        return await query.OrderBy(f => f.FechaServicio).ToListAsync();
    }

    public async Task<bool> IncrementarSolicitadosAsync(int servicioId, int cantidad = 1)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        var servicio = await context.Servicio.FirstOrDefaultAsync(s => s.ServicioId == servicioId);

        if (servicio == null)
            return false;

        servicio.Solicitados += cantidad;

        return await context.SaveChangesAsync() > 0;
    }

    public double CalcularGananciaServicio(Servicios servicio)
    {
        return servicio.Precio * servicio.Solicitados;
    }

    public double CalcularTotalGanancia(List<Servicios> listaServicios)
    {
        return listaServicios.Sum(s => CalcularGananciaServicio(s));
    }
}
