using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Components.Models.Paginacion;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsAP1.Components.Service;

public class CitaService(IDbContextFactory<ApplicationDbContext> DbFactory)
{

    public async Task<bool> GuardarCitaAsync(Cita cita)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        try
        {
            if (cita.CitaId == 0)
            {
                cita.CodigoConfirmacion = GenerateUniqueCode();
                context.Citas.Add(cita);
            }
            else
            {
                context.Citas.Update(cita);
            }
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al guardar la cita: {ex.Message}");
            return false;
        }
    }

    public async Task<Cita?> GetCitaByIdAsync(int citaId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Citas
                             .FirstOrDefaultAsync(c => c.CitaId == citaId);
    }

    // Método para confirmar una cita
    public async Task<bool> ConfirmarCitaAsync(int citaId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        try
        {
            var cita = await context.Citas.FindAsync(citaId);
            if (cita == null)
            {
                return false;
            }
            cita.Confirmada = true;
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public async Task<bool> EliminarCitaAsync(Cita cita)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        try
        {
            context.Citas.Remove(cita);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<PaginacionResultado<Cita>> BuscarCitasAsync(string filtro, string valorFiltro, DateTime? fechaDesde, DateTime? fechaHasta, int pagina, int tamañoPagina)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        var query = context.Citas.AsQueryable();

        if (!string.IsNullOrEmpty(filtro) && !string.IsNullOrEmpty(valorFiltro))
        {
            switch (filtro.ToLower())
            {
                case "citaid":
                    if (int.TryParse(valorFiltro, out int id))
                        query = query.Where(c => c.CitaId == id);
                    break;
                case "clientenombre":
                    query = query.Where(c => c.ClienteNombre.ToLower().Contains(valorFiltro.ToLower()));
                    break;
                case "serviciosolicitado":
                    query = query.Where(c => c.ServicioSolicitado.ToLower().Contains(valorFiltro.ToLower()));
                    break;
                case "codigoconfirmacion":
                    query = query.Where(c => c.CodigoConfirmacion.ToLower().Contains(valorFiltro.ToLower()));
                    break;
            }
        }

        if (fechaDesde.HasValue)
            query = query.Where(c => c.FechaCita >= fechaDesde.Value);
        if (fechaHasta.HasValue)
            query = query.Where(c => c.FechaCita <= fechaHasta.Value);

        query = query.OrderByDescending(c => c.FechaCita);

        var totalItems = await query.CountAsync();
        var citasPaginadas = await query
            .Skip((pagina - 1) * tamañoPagina)
            .Take(tamañoPagina)
            .AsNoTracking()
            .ToListAsync();

        return new PaginacionResultado<Cita>
        {
            Items = citasPaginadas,
            TotalPaginas = (int)Math.Ceiling(totalItems / (double)tamañoPagina),
            PaginaActual = pagina
        };
    }

    public async Task<List<Cita>> GetCitasPorUsuarioIdAsync(string userId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Citas
                             .Where(c => c.ApplicationUserId == userId)
                             .OrderByDescending(c => c.FechaCita)
                             .ToListAsync();
    }

    private string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    }
}

