using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsAP1.Components.Services;

public class CarritoService(IDbContextFactory<ApplicationDbContext> DbFactory)
{
    private List<Carrito> _cartItems = new List<Carrito>();

    public event Action? OnCartChanged;

    public List<Carrito> GetCartItems() => _cartItems;

    public int GetTotalItems() => _cartItems.Sum(item => item.Cantidad);

    public double GetTotalPrice() => _cartItems.Sum(item => item.Subtotal);

    public void AddItem(Productos producto, int cantidad = 1)
    {
        var existingItem = _cartItems.FirstOrDefault(item => item.Producto.ProductoId == producto.ProductoId);

        if (existingItem != null)
        {
            existingItem.Cantidad += cantidad;
        }
        else
        {
            _cartItems.Add(new Carrito { Producto = producto, Cantidad = cantidad });
        }
        OnCartChanged?.Invoke(); 
    }

    public void RemoveItem(int productoId)
    {
        _cartItems.RemoveAll(item => item.Producto.ProductoId == productoId);
        OnCartChanged?.Invoke();
    }

    public void UpdateQuantity(int productoId, int newQuantity)
    {
        var existingItem = _cartItems.FirstOrDefault(item => item.Producto.ProductoId == productoId);
        if (existingItem != null)
        {
            if (newQuantity <= 0)
            {
                _cartItems.Remove(existingItem);
            }
            else
            {
                existingItem.Cantidad = newQuantity;
            }
            OnCartChanged?.Invoke();
        }
    }

    public void ClearCart()
    {
        _cartItems.Clear();
        OnCartChanged?.Invoke();
    }
}