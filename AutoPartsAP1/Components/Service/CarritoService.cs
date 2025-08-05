using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsAP1.Components.Services;

public class CarritoService
{
    private readonly IDbContextFactory<ApplicationDbContext> DbFactory;
    private List<Carrito> _cartItems = new List<Carrito>();

    public event Action? OnCartChanged;

    public CarritoService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        DbFactory = dbFactory;
    }

    public List<Carrito> GetCartItems() => _cartItems;

    public int GetTotalItems() => _cartItems.Sum(item => item.Cantidad);

    public double GetTotalPrice() => _cartItems.Sum(item => item.Subtotal);

    public async Task<Productos?> ObtenerProductoPorIdAsync(int productoId)
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        return await context.Producto.FirstOrDefaultAsync(p => p.ProductoId == productoId);
    }

    public async Task<bool> AddItemConValidacionAsync(int productoId, int cantidad)
    {
        if (cantidad <= 0) return false;

        var producto = await ObtenerProductoPorIdAsync(productoId);
        if (producto == null) return false;

        var existingItem = _cartItems.FirstOrDefault(item => item.Producto.ProductoId == productoId);
        var cantidadActualEnCarrito = existingItem?.Cantidad ?? 0;
        var cantidadTotal = cantidadActualEnCarrito + cantidad;

        if (cantidadTotal > producto.ProductoCantidad)
        {
            return false;
        }

        if (existingItem != null)
        {
            existingItem.Cantidad = cantidadTotal;
        }
        else
        {
            _cartItems.Add(new Carrito { Producto = producto, Cantidad = cantidad });
        }

        OnCartChanged?.Invoke();
        return true;
    }

    public void RemoveItem(int productoId)
    {
        _cartItems.RemoveAll(item => item.Producto.ProductoId == productoId);
        OnCartChanged?.Invoke();
    }

    public async Task<bool> UpdateQuantityConValidacionAsync(int productoId, int newQuantity)
    {
        var producto = await ObtenerProductoPorIdAsync(productoId);
        if (producto == null)
            return false;

        if (newQuantity <= 0)
        {
            RemoveItem(productoId);
            OnCartChanged?.Invoke();
            return true;
        }

        if (newQuantity > producto.ProductoCantidad)
        {
            return false;
        }

        var existingItem = _cartItems.FirstOrDefault(i => i.Producto.ProductoId == productoId);
        if (existingItem != null)
        {
            existingItem.Cantidad = newQuantity;
            OnCartChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void ClearCart()
    {
        _cartItems.Clear();
        OnCartChanged?.Invoke();
    }
}