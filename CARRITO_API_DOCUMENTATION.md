# API de Carrito - Documentación

## Descripción General
API RESTful para gestionar el carrito de compras de los usuarios. Todos los endpoints requieren autenticación mediante JWT Bearer Token.

## Base URL
```
https://localhost:7053/api/carrito
```

## Autenticación
Todos los endpoints requieren un token JWT válido en el header:
```
Authorization: Bearer {token}
```

---

## Endpoints

### 1. Obtener Carrito del Usuario

**GET** `/api/carrito`

Obtiene todos los items del carrito del usuario autenticado.

**Respuesta Exitosa (200 OK):**
```json
[
  {
    "carritoId": 1,
    "applicationUserId": "user-id-123",
    "productoId": 5,
    "producto": {
      "productoId": 5,
      "productoNombre": "Filtro de Aceite",
      "productoMonto": 450.00,
      "productoCantidad": 50,
      "productoDescripcion": "Filtro de aceite premium",
      "productoImagenUrl": "https://example.com/image.jpg",
      "categoria": "Repuestos",
      "fecha": "2024-01-15T00:00:00"
    },
    "cantidad": 2
  }
]
```

---

### 2. Obtener Total del Carrito

**GET** `/api/carrito/total`

Obtiene el resumen del carrito: cantidad total de items y precio total.

**Respuesta Exitosa (200 OK):**
```json
{
  "totalItems": 5,
  "totalPrice": 3500.00
}
```

---

### 3. Agregar Producto al Carrito

**POST** `/api/carrito`

Agrega un producto al carrito o incrementa la cantidad si ya existe.

**Body (JSON):**
```json
{
  "productoId": 5,
  "cantidad": 2
}
```

**Validaciones:**
- `productoId`: Requerido, debe existir en la base de datos
- `cantidad`: Requerido, debe ser mayor a 0 y no exceder el stock disponible

**Respuesta Exitosa (201 Created):**
```json
{
  "carritoId": 1,
  "applicationUserId": "user-id-123",
  "productoId": 5,
  "producto": {
    "productoId": 5,
    "productoNombre": "Filtro de Aceite",
    "productoMonto": 450.00,
    "productoCantidad": 50,
    "productoDescripcion": "Filtro de aceite premium",
    "productoImagenUrl": "https://example.com/image.jpg",
    "categoria": "Repuestos",
    "fecha": "2024-01-15T00:00:00"
  },
  "cantidad": 2
}
```

**Errores Posibles:**
- `400 Bad Request`: Cantidad inválida o stock insuficiente
- `404 Not Found`: Producto no encontrado

---

### 4. Actualizar Cantidad de Item

**PUT** `/api/carrito/{carritoId}`

Actualiza la cantidad de un item específico del carrito.

**Parámetros:**
- `carritoId`: ID del item en el carrito

**Body (JSON):**
```json
{
  "cantidad": 5
}
```

**Validaciones:**
- `cantidad`: Debe ser mayor a 0 y no exceder el stock disponible

**Respuesta Exitosa (204 No Content)**

**Errores Posibles:**
- `400 Bad Request`: Cantidad inválida o stock insuficiente
- `404 Not Found`: Item no encontrado o no pertenece al usuario

---

### 5. Eliminar Item del Carrito

**DELETE** `/api/carrito/{carritoId}`

Elimina un item específico del carrito.

**Parámetros:**
- `carritoId`: ID del item en el carrito

**Respuesta Exitosa (204 No Content)**

**Errores Posibles:**
- `404 Not Found`: Item no encontrado o no pertenece al usuario

---

### 6. Vaciar Carrito

**DELETE** `/api/carrito/clear`

Elimina todos los items del carrito del usuario autenticado.

**Respuesta Exitosa (204 No Content)**

---

## Códigos de Estado HTTP

| Código | Descripción |
|--------|-------------|
| 200 OK | Solicitud exitosa |
| 201 Created | Recurso creado exitosamente |
| 204 No Content | Operación exitosa sin contenido de respuesta |
| 400 Bad Request | Datos inválidos en la solicitud |
| 401 Unauthorized | No autenticado o token inválido |
| 404 Not Found | Recurso no encontrado |
| 500 Internal Server Error | Error del servidor |

---

## Ejemplos de Uso

### Flujo Típico de Compra

1. **Login del usuario** para obtener el token JWT
2. **Agregar productos al carrito:**
```bash
POST /api/carrito
{
  "productoId": 5,
  "cantidad": 2
}
```

3. **Consultar el carrito:**
```bash
GET /api/carrito
```

4. **Actualizar cantidades si es necesario:**
```bash
PUT /api/carrito/1
{
  "cantidad": 3
}
```

5. **Ver el total antes de proceder al pago:**
```bash
GET /api/carrito/total
```

6. **Procesar la compra** (usar API de Ventas)

7. **Limpiar el carrito después de la compra:**
```bash
DELETE /api/carrito/clear
```

---

## Notas Importantes

- El carrito es personal para cada usuario autenticado
- Los items del carrito persisten en la base de datos
- Se valida automáticamente el stock disponible al agregar o actualizar items
- Si un producto ya está en el carrito y se vuelve a agregar, se incrementa su cantidad
- Todos los endpoints están protegidos con autenticación JWT

---

## Errores Comunes

### Stock Insuficiente
```json
{
  "error": "Stock insuficiente. Disponible: 10"
}
```

### Usuario No Autenticado
```json
{
  "error": "Usuario no autenticado"
}
```

### Producto No Encontrado
```json
{
  "error": "Producto con ID 5 no encontrado"
}
```

---

## Integración con App Móvil

Para integrar con tu aplicación móvil:

1. Implementa el manejo de tokens JWT en tu app
2. Almacena el token de forma segura después del login
3. Incluye el token en todas las peticiones al carrito
4. Maneja los estados de carga y errores apropiadamente
5. Sincroniza el carrito al iniciar sesión
6. Actualiza la UI en tiempo real cuando cambien los items del carrito

### Ejemplo con HttpClient (C#):
```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync("https://api.example.com/api/carrito");
var items = await response.Content.ReadFromJsonAsync<List<CarritoDto>>();
```
