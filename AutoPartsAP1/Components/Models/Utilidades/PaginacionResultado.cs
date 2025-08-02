namespace AutoPartsAP1.Components.Models.Paginacion;

public class PaginacionResultado<T>
{
    public List<T> Items { get; set; } = new();
    public int PaginaActual { get; set; }
    public int TotalPaginas { get; set; }

    public bool TienePaginaAnterior => PaginaActual > 1;
    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}