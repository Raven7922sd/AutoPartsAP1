# Guía Rápida - API de Usuarios

## ?? Endpoints SIN Autenticación (Públicos)

### 1. Verificar que el API funcione
```bash
GET /api/Users/test
```

**Respuesta:**
```json
{
  "status": "OK",
  "message": "Users API está funcionando correctamente",
  "timestamp": "2025-11-28T20:06:20Z",
  "endpoints": {
    "register": "POST /api/Users/register",
    "login": "POST /api/Users/login",
    "getUsers": "GET /api/Users (requiere Admin)",
    "getUser": "GET /api/Users/{id} (requiere autenticación)",
    "count": "GET /api/Users/count (público)"
  }
}
```

### 2. Contar usuarios
```bash
GET /api/Users/count
```

**Respuesta:**
```json
{
  "totalUsers": 5,
  "message": "API funcionando correctamente",
  "timestamp": "2025-11-28T20:06:20Z"
}
```

### 3. Registrar Usuario
```bash
POST /api/Users/register
Content-Type: application/json

{
  "email": "nuevo@example.com",
  "password": "Password123!",
  "phoneNumber": "555-1234"
}
```

**Respuesta (201 Created):**
```json
{
  "id": "abc123-guid",
  "userName": "nuevo@example.com",
  "email": "nuevo@example.com",
  "phoneNumber": "555-1234",
  "emailConfirmed": true,
  "phoneNumberConfirmed": false,
  "twoFactorEnabled": false,
  "lockoutEnabled": true,
  "lockoutEnd": null,
  "accessFailedCount": 0,
  "roles": ["User"]
}
```

### 4. Login
```bash
POST /api/Users/login
Content-Type: application/json

{
  "email": "nuevo@example.com",
  "password": "Password123!"
}
```

**Respuesta (200 OK):**
```json
{
  "id": "abc123-guid",
  "userName": "nuevo@example.com",
  "email": "nuevo@example.com",
  "phoneNumber": "555-1234",
  "emailConfirmed": true,
  "roles": ["User"]
}
```

---

## ?? Endpoints CON Autenticación

### 5. Obtener Usuario por ID (Requiere estar autenticado)
```bash
GET /api/Users/{id}
Authorization: Bearer {token}
```

### 6. Obtener Usuario por Email (Requiere estar autenticado)
```bash
GET /api/Users/email/usuario@example.com
Authorization: Bearer {token}
```

### 7. Actualizar Usuario (Requiere estar autenticado)
```bash
PUT /api/Users/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "nuevoemail@example.com",
  "phoneNumber": "555-9999",
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

---

## ?? Endpoints Solo para ADMIN

### 8. Obtener Todos los Usuarios
```bash
GET /api/Users
Authorization: Bearer {admin-token}
```

### 9. Eliminar Usuario
```bash
DELETE /api/Users/{id}
Authorization: Bearer {admin-token}
```

---

## ?? Pruebas en Swagger

1. **Ejecuta el API:**
   ```bash
   cd AutoParts.Api
   dotnet run
   ```

2. **Abre Swagger:**
   - Local: `http://localhost:5022`
   - HTTPS: `https://localhost:7240`

3. **Prueba en este orden:**
   1. `GET /api/Users/test` ? (verifica que funcione)
   2. `GET /api/Users/count` ? (cuenta usuarios)
   3. `POST /api/Users/register` ? (crea un usuario)
   4. `POST /api/Users/login` ? (inicia sesión)
   5. Guarda el `id` del usuario devuelto
   6. `GET /api/Users/{id}` ? (dará 401 - necesita autenticación)

---

## ?? Uso en App Móvil Android

### Paso 1: Registrar Usuario
```kotlin
suspend fun registerUser(email: String, password: String, phone: String?): UserDto? {
    val response = client.post("https://tu-api.com/api/Users/register") {
        contentType(ContentType.Application.Json)
        setBody(CreateUserDto(email, password, phone))
    }
    
    return if (response.status == HttpStatusCode.Created) {
        response.body<UserDto>()
    } else {
        null
    }
}
```

### Paso 2: Login
```kotlin
suspend fun login(email: String, password: String): UserDto? {
    val response = client.post("https://tu-api.com/api/Users/login") {
        contentType(ContentType.Application.Json)
        setBody(LoginDto(email, password))
    }
    
    return if (response.status == HttpStatusCode.OK) {
        val user = response.body<UserDto>()
        // Guardar el userId en SharedPreferences
        saveUserId(user.id)
        user
    } else {
        null
    }
}
```

### Paso 3: Verificar Conexión (Opcional)
```kotlin
suspend fun checkApiConnection(): Boolean {
    return try {
        val response = client.get("https://tu-api.com/api/Users/test")
        response.status == HttpStatusCode.OK
    } catch (e: Exception) {
        false
    }
}
```

---

## ? Solución al Error 401

El error **401 Unauthorized** que estabas viendo se debe a que el endpoint `GET /api/Users` requiere:

1. **Estar autenticado** (tener un token válido)
2. **Tener rol de Admin**

### Soluciones:

#### Opción 1: Usa los endpoints públicos
```bash
# En lugar de:
GET /api/Users  ? (requiere Admin)

# Usa:
GET /api/Users/test  ? (público)
GET /api/Users/count ? (público)
```

#### Opción 2: Registra un usuario primero
```bash
1. POST /api/Users/register
2. POST /api/Users/login
3. Guarda el ID del usuario
4. GET /api/Users/{id} (con autenticación)
```

#### Opción 3: Para desarrollo, haz el endpoint público temporalmente
Si quieres ver todos los usuarios durante el desarrollo, puedes cambiar:

```csharp
// De:
[HttpGet]
[Authorize(Roles = "Admin")]

// A:
[HttpGet]
[AllowAnonymous] // Solo para desarrollo
```

---

## ?? Checklist para tu App Móvil

- [ ] Probar `GET /api/Users/test` - Verificar conexión
- [ ] Probar `GET /api/Users/count` - Ver cuántos usuarios hay
- [ ] Implementar registro: `POST /api/Users/register`
- [ ] Implementar login: `POST /api/Users/login`
- [ ] Guardar el `userId` devuelto en SharedPreferences
- [ ] Usar el `userId` para obtener datos del usuario cuando sea necesario

---

## ?? Notas Importantes

1. **Para apps móviles**, los endpoints importantes son:
   - `POST /api/Users/register` - Crear cuenta
   - `POST /api/Users/login` - Iniciar sesión
   - `GET /api/Users/{id}` - Obtener perfil (necesita auth)
   - `PUT /api/Users/{id}` - Actualizar perfil (necesita auth)

2. **No necesitas** implementar autenticación JWT ahora mismo para probar el registro y login.

3. **El userId** que devuelve el login es lo que necesitas guardar para identificar al usuario en la app.

4. **Los endpoints de Admin** solo los usarás desde el panel web de Blazor, no desde la app móvil.
