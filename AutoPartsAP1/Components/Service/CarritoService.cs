using AutoPartsAP1.Components.Models;
using AutoPartsAP1.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoPartsAP1.Components.Services;

public class CarritoService
{
    private readonly IDbContextFactory<ApplicationDbContext> DbFactory;
    private readonly AuthenticationStateProvider AuthenticationStateProvider;
    private List<Carrito> _cartItems = new List<Carrito>();
    private string? _userId;

    public event Action? OnCartChanged;

    public CarritoService(IDbContextFactory<ApplicationDbContext> dbFactory, AuthenticationStateProvider authenticationStateProvider)
    {
        DbFactory = dbFactory;
        AuthenticationStateProvider = authenticationStateProvider;
        LoadUser();
    }

    private async void LoadUser()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        if (user.Identity?.IsAuthenticated ?? false)
        {
            _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (_userId != null)
            {
                await LoadCartFromDatabaseAsync();
            }
        }
    }

    private async Task LoadCartFromDatabaseAsync()
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        var dbItems = await context.CarritoItems
            .Where(item => item.ApplicationUserId == _userId)
            .Include(item => item.Producto)
            .AsNoTracking()
            .ToListAsync();

        _cartItems = dbItems.Select(item => new Carrito
        {
            Producto = item.Producto,
            Cantidad = item.Cantidad
        }).ToList();

        OnCartChanged?.Invoke();
    }

    private async Task SaveCartToDatabaseAsync()
    {
        if (_userId == null) return;

        await using var context = await DbFactory.CreateDbContextAsync();

        // Elimina los items actuales del carrito del usuario
        var existingItems = await context.CarritoItems.Where(i => i.ApplicationUserId == _userId).ToListAsync();
        context.CarritoItems.RemoveRange(existingItems);

        // Agrega los nuevos items
        var newItems = _cartItems.Select(item => new Carrito
        {
            ApplicationUserId = _userId,
            ProductoId = item.Producto.ProductoId,
            Cantidad = item.Cantidad
        }).ToList();

        context.CarritoItems.AddRange(newItems);

        await context.SaveChangesAsync();
    }

    public List<Carrito> GetCartItems() => _cartItems;
    public int GetTotalItems() => _cartItems.Sum(item => item.Cantidad);
    public double GetTotalPrice() => _cartItems.Sum(item => item.Producto.ProductoMonto);

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

        if (_userId != null)
        {
            await SaveCartToDatabaseAsync();
        }

        OnCartChanged?.Invoke();
        return true;
    }

    public async void RemoveItem(int productoId)
    {
        _cartItems.RemoveAll(item => item.Producto.ProductoId == productoId);

        if (_userId != null)
        {
            await SaveCartToDatabaseAsync();
        }

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

            if (_userId != null)
            {
                await SaveCartToDatabaseAsync();
            }

            OnCartChanged?.Invoke();
            return true;
        }
        return false;
    }

    public async void ClearCart()
    {
        _cartItems.Clear();

        if (_userId != null)
        {
            await SaveCartToDatabaseAsync();
        }

        OnCartChanged?.Invoke();
    }
}