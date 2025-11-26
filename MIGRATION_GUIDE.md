# AutoParts - Proyecto Compartido

## Estructura del Proyecto

```
AutoPartsAP1.sln
??? AutoParts.Shared/          # Proyecto compartido (Class Library)
?   ??? Data/                  # DbContext y ApplicationUser
?   ??? Models/                # Modelos de entidades
?   ??? DTOs/                  # Data Transfer Objects
?   ??? Services/              # Servicios de negocio
?   ??? Extensions/            # Extensiones y utilidades
??? AutoPartsAP1/              # Proyecto Blazor Server
?   ??? Components/            # Componentes Blazor
??? AutoParts.Api/             # Proyecto Web API
    ??? Controllers/           # Controladores REST API
```

## Cambios Realizados

### 1. AutoParts.Shared (Nuevo)
**Contiene:**
- `ApplicationDbContext` y `ApplicationUser` migrados
- Todos los modelos: `Productos`, `Ventas`, `VentasDetalles`, `PagoModel`, `Servicios`, `Cita`, `Carrito`
- Todos los DTOs: `VentaDto`, `VentaDetalleDto`, `FacturaDto`, `FacturaDetalleDto`
- Todos los servicios: `ProductoService`, `VentasService`, `ComprasService`, `ServiciosService`, `CitaService`, `CarritoService`
- Utilidades de paginación y extensiones

**Paquetes instalados:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.7" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="9.0.7" />
```

### 2. AutoPartsAP1 (Modificado)
**Cambios:**
- Referencia al proyecto `AutoParts.Shared`
- `Program.cs` actualizado para usar `AutoParts.Shared.Data.ApplicationDbContext` y servicios compartidos
- `_Imports.razor` actualizado con namespaces compartidos
- Archivos de Identity actualizados para usar `AutoParts.Shared.Data.ApplicationUser`

**IMPORTANTE:** 
- Los archivos antiguos en `Data/` y `Components/Models/` y `Components/Service/` ya NO se usan
- Puedes eliminarlos manualmente después de verificar que todo funciona

### 3. AutoParts.Api (Nuevo)
**Configurado con:**
- Referencia al proyecto `AutoParts.Shared`
- `ApplicationDbContext` y servicios compartidos registrados
- Identity configurado para autenticación API
- CORS habilitado para Blazor client
- Swagger/OpenAPI configurado
- Controlador de ejemplo: `ProductosController`

## Cómo Compilar

```bash
# Restaurar paquetes
dotnet restore

# Compilar toda la solución
dotnet build

# Compilar proyecto específico
dotnet build AutoParts.Shared/AutoParts.Shared.csproj
dotnet build AutoPartsAP1/AutoPartsAP1.csproj
dotnet build AutoParts.Api/AutoParts.Api.csproj
```

## Cómo Ejecutar

### Blazor Server
```bash
cd AutoPartsAP1
dotnet run
```
URL: `https://localhost:7001` (o el puerto configurado)

### Web API
```bash
cd AutoParts.Api
dotnet run
```
URL: `https://localhost:7002` (o el puerto configurado)
Swagger: `https://localhost:7002/swagger`

## Migraciones

Las migraciones existentes están en el proyecto `AutoPartsAP1`. Para futuras migraciones:

```bash
# Crear migración desde AutoPartsAP1
dotnet ef migrations add NombreMigracion --project AutoParts.Shared --startup-project AutoPartsAP1

# Aplicar migración
dotnet ef database update --project AutoParts.Shared --startup-project AutoPartsAP1
```

O desde la API:
```bash
dotnet ef migrations add NombreMigracion --project AutoParts.Shared --startup-project AutoParts.Api
dotnet ef database update --project AutoParts.Shared --startup-project AutoParts.Api
```

## Archivos a Eliminar (Después de verificar)

En `AutoPartsAP1`:
- `Data/ApplicationDbContext.cs` ?
- `Data/ApplicationUser.cs` ?
- `Components/Models/*` (todos los archivos) ?
- `Components/Service/*` (todos los archivos) ?
- `Components/Services/*` (todos los archivos) ?

**NOTA:** Antes de eliminar, asegúrate de que tu aplicación compile y funcione correctamente.

## Configuración CORS en la API

Actualiza las URLs del cliente Blazor en `AutoParts.Api/Program.cs`:

```csharp
options.AddPolicy("AllowBlazorClient", policy =>
{
    policy.WithOrigins("https://localhost:7001", "http://localhost:5001") // <-- Ajusta estos puertos
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
});
```

## Próximos Pasos

1. ? Compilar la solución: `dotnet build`
2. ? Verificar que Blazor funciona correctamente
3. ? Probar la API con Swagger
4. ?? Crear más controladores para Ventas, Servicios, Citas, etc.
5. ?? Implementar autenticación JWT si quieres consumir la API desde otro cliente
6. ?? Eliminar archivos duplicados del proyecto Blazor

## Estructura de Namespaces

| Antes | Ahora |
|-------|-------|
| `using AutoPartsAP1.Data;` | `using AutoParts.Shared.Data;` |
| `using AutoPartsAP1.Components.Models;` | `using AutoParts.Shared.Data;` |
| `using AutoPartsAP1.Components.Services;` | `using AutoParts.Shared.Services;` |
| `using AutoPartsAP1.Components.Service;` | `using AutoParts.Shared.Services;` |
| N/A | `using AutoParts.Shared.DTOs;` |

## Endpoints API Disponibles

### Productos
- `GET /api/productos` - Listar todos los productos
- `GET /api/productos/{id}` - Obtener producto por ID
- `POST /api/productos` - Crear producto (requiere autenticación)
- `PUT /api/productos/{id}` - Actualizar producto (requiere autenticación)
- `DELETE /api/productos/{id}` - Eliminar producto (requiere autenticación)

### Identity
- `POST /register` - Registrar usuario
- `POST /login` - Iniciar sesión
- Consulta la documentación de ASP.NET Core Identity para más endpoints

## Troubleshooting

### Error: "No se encuentra el namespace AutoParts.Shared"
**Solución:** Asegúrate de haber hecho `dotnet build` en el proyecto `AutoParts.Shared` primero.

### Error: "Duplicate definition of ApplicationDbContext"
**Solución:** Elimina el archivo `Data/ApplicationDbContext.cs` del proyecto `AutoPartsAP1`.

### Error de CORS en la API
**Solución:** Verifica que las URLs en `Program.cs` de la API coincidan con las del cliente Blazor.

### Migraciones no funcionan
**Solución:** Usa el parámetro `--startup-project` para especificar cuál proyecto contiene la cadena de conexión.

## Contacto y Soporte

Si tienes problemas, revisa:
1. Los logs de compilación: `dotnet build`
2. Los logs de ejecución en la consola
3. Swagger para probar la API: `/swagger`
