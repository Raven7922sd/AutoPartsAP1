# API de Usuarios - Documentación

## Endpoints Disponibles

### 1. **Registrar Usuario** (POST)
```
POST /api/Users/register
```

**Body (JSON):**
```json
{
  "email": "usuario@example.com",
  "password": "Password123!",
  "phoneNumber": "555-1234" // Opcional
}
```

**Respuesta Exitosa (201):**
```json
{
  "id": "abc123-guid",
  "userName": "usuario@example.com",
  "email": "usuario@example.com",
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

---

### 2. **Iniciar Sesión** (POST)
```
POST /api/Users/login
```

**Body (JSON):**
```json
{
  "email": "usuario@example.com",
  "password": "Password123!"
}
```

**Respuesta Exitosa (200):**
```json
{
  "id": "abc123-guid",
  "userName": "usuario@example.com",
  "email": "usuario@example.com",
  "phoneNumber": "555-1234",
  "emailConfirmed": true,
  "roles": ["User"]
}
```

---

### 3. **Obtener Todos los Usuarios** (GET) - Solo Admin
```
GET /api/Users
Authorization: Bearer {token}
```

**Respuesta Exitosa (200):**
```json
[
  {
    "id": "abc123-guid",
    "userName": "usuario@example.com",
    "email": "usuario@example.com",
    "phoneNumber": "555-1234",
    "emailConfirmed": true,
    "roles": ["User"]
  }
]
```

---

### 4. **Obtener Usuario por ID** (GET)
```
GET /api/Users/{id}
Authorization: Bearer {token}
```

**Respuesta Exitosa (200):**
```json
{
  "id": "abc123-guid",
  "userName": "usuario@example.com",
  "email": "usuario@example.com",
  "phoneNumber": "555-1234",
  "emailConfirmed": true,
  "roles": ["User"]
}
```

---

### 5. **Obtener Usuario por Email** (GET)
```
GET /api/Users/email/{email}
Authorization: Bearer {token}
```

**Respuesta Exitosa (200):**
```json
{
  "id": "abc123-guid",
  "userName": "usuario@example.com",
  "email": "usuario@example.com",
  "phoneNumber": "555-1234",
  "emailConfirmed": true,
  "roles": ["User"]
}
```

---

### 6. **Actualizar Usuario** (PUT)
```
PUT /api/Users/{id}
Authorization: Bearer {token}
```

**Body (JSON):**
```json
{
  "email": "nuevoemail@example.com", // Opcional
  "phoneNumber": "555-9999", // Opcional
  "currentPassword": "Password123!", // Opcional - requerido para cambiar contraseña
  "newPassword": "NewPassword123!" // Opcional
}
```

**Respuesta Exitosa (204):** No Content

---

### 7. **Eliminar Usuario** (DELETE) - Solo Admin
```
DELETE /api/Users/{id}
Authorization: Bearer {token}
```

**Respuesta Exitosa (204):** No Content

---

## Ejemplo de uso en Android (Kotlin)

### Modelo de Datos
```kotlin
data class UserDto(
    val id: String,
    val userName: String?,
    val email: String?,
    val phoneNumber: String?,
    val emailConfirmed: Boolean,
    val roles: List<String>
)

data class CreateUserDto(
    val email: String,
    val password: String,
    val phoneNumber: String? = null
)

data class LoginDto(
    val email: String,
    val password: String
)

data class UpdateUserDto(
    val email: String? = null,
    val phoneNumber: String? = null,
    val currentPassword: String? = null,
    val newPassword: String? = null
)
```

### Registrar Usuario
```kotlin
suspend fun registerUser(email: String, password: String, phoneNumber: String? = null): UserDto? {
    val createUserDto = CreateUserDto(email, password, phoneNumber)
    
    val response = client.post("https://tu-api.com/api/Users/register") {
        contentType(ContentType.Application.Json)
        setBody(createUserDto)
    }
    
    return if (response.status == HttpStatusCode.Created) {
        response.body<UserDto>()
    } else {
        null
    }
}
```

### Iniciar Sesión
```kotlin
suspend fun login(email: String, password: String): UserDto? {
    val loginDto = LoginDto(email, password)
    
    val response = client.post("https://tu-api.com/api/Users/login") {
        contentType(ContentType.Application.Json)
        setBody(loginDto)
    }
    
    return if (response.status == HttpStatusCode.OK) {
        val user = response.body<UserDto>()
        // Guardar el ID del usuario en SharedPreferences
        saveUserId(user.id)
        user
    } else {
        null
    }
}
```

### Obtener Usuario
```kotlin
suspend fun getUser(userId: String): UserDto? {
    val response = client.get("https://tu-api.com/api/Users/$userId") {
        // Si tienes autenticación con token:
        // bearerAuth(token)
    }
    
    return if (response.status == HttpStatusCode.OK) {
        response.body<UserDto>()
    } else {
        null
    }
}
```

### Actualizar Usuario
```kotlin
suspend fun updateUser(
    userId: String, 
    email: String? = null,
    phoneNumber: String? = null,
    currentPassword: String? = null,
    newPassword: String? = null
): Boolean {
    val updateUserDto = UpdateUserDto(email, phoneNumber, currentPassword, newPassword)
    
    val response = client.put("https://tu-api.com/api/Users/$userId") {
        contentType(ContentType.Application.Json)
        setBody(updateUserDto)
    }
    
    return response.status == HttpStatusCode.NoContent
}
```

### Eliminar Usuario (Solo Admin)
```kotlin
suspend fun deleteUser(userId: String): Boolean {
    val response = client.delete("https://tu-api.com/api/Users/$userId") {
        // bearerAuth(adminToken)
    }
    
    return response.status == HttpStatusCode.NoContent
}
```

---

## Ejemplo de uso en Flutter (Dart)

### Modelos
```dart
class UserDto {
  final String id;
  final String? userName;
  final String? email;
  final String? phoneNumber;
  final bool emailConfirmed;
  final List<String> roles;

  UserDto({
    required this.id,
    this.userName,
    this.email,
    this.phoneNumber,
    required this.emailConfirmed,
    required this.roles,
  });

  factory UserDto.fromJson(Map<String, dynamic> json) {
    return UserDto(
      id: json['id'],
      userName: json['userName'],
      email: json['email'],
      phoneNumber: json['phoneNumber'],
      emailConfirmed: json['emailConfirmed'],
      roles: List<String>.from(json['roles']),
    );
  }
}
```

### Registrar Usuario
```dart
Future<UserDto?> registerUser(String email, String password, {String? phoneNumber}) async {
  final response = await http.post(
    Uri.parse('https://tu-api.com/api/Users/register'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'email': email,
      'password': password,
      if (phoneNumber != null) 'phoneNumber': phoneNumber,
    }),
  );

  if (response.statusCode == 201) {
    return UserDto.fromJson(jsonDecode(response.body));
  }
  return null;
}
```

### Iniciar Sesión
```dart
Future<UserDto?> login(String email, String password) async {
  final response = await http.post(
    Uri.parse('https://tu-api.com/api/Users/login'),
    headers: {'Content-Type': 'application/json'},
    body: jsonEncode({
      'email': email,
      'password': password,
    }),
  );

  if (response.statusCode == 200) {
    return UserDto.fromJson(jsonDecode(response.body));
  }
  return null;
}
```

---

## Notas Importantes

1. **Autenticación**: Para usar los endpoints protegidos, necesitarás implementar autenticación con tokens JWT o similar.
2. **Roles**: Por defecto, los usuarios registrados obtienen el rol "User". Solo los administradores pueden ver todos los usuarios y eliminarlos.
3. **Validaciones**: El email debe ser válido y la contraseña debe tener al menos 6 caracteres.
4. **Confirmación de Email**: Por defecto, los emails se auto-confirman (`EmailConfirmed = true`) para facilitar el uso en apps móviles.

---

## Códigos de Estado HTTP

- **200 OK**: Operación exitosa (GET, Login)
- **201 Created**: Usuario creado exitosamente
- **204 No Content**: Operación exitosa sin contenido (PUT, DELETE)
- **400 Bad Request**: Datos inválidos
- **401 Unauthorized**: No autenticado o credenciales incorrectas
- **403 Forbidden**: No tiene permisos
- **404 Not Found**: Usuario no encontrado
- **500 Internal Server Error**: Error del servidor
