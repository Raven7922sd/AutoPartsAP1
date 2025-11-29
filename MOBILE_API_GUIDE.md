# API de Usuarios - Guía para App Móvil

## ?? Quick Start - Solo Email y Contraseña

Tu app móvil solo necesita enviar **email** y **contraseña**. El teléfono es **OPCIONAL**.

---

## ? Registrar Usuario (Solo Email y Contraseña)

### Request
```http
POST /api/Users/register
Content-Type: application/json

{
  "email": "usuario@example.com",
  "password": "Password123!"
}
```

### Response (201 Created)
```json
{
  "id": "abc123-guid-here",
  "userName": "usuario@example.com",
  "email": "usuario@example.com",
  "phoneNumber": null,
  "emailConfirmed": true,
  "phoneNumberConfirmed": false,
  "twoFactorEnabled": false,
  "lockoutEnabled": true,
  "lockoutEnd": null,
  "accessFailedCount": 0,
  "roles": ["User"]
}
```

**Importante:** Guarda el `id` que devuelve. Lo necesitarás para obtener o actualizar el perfil del usuario.

---

## ? Login (Solo Email y Contraseña)

### Request
```http
POST /api/Users/login
Content-Type: application/json

{
  "email": "usuario@example.com",
  "password": "Password123!"
}
```

### Response (200 OK)
```json
{
  "id": "abc123-guid-here",
  "userName": "usuario@example.com",
  "email": "usuario@example.com",
  "phoneNumber": null,
  "emailConfirmed": true,
  "roles": ["User"]
}
```

**Importante:** Guarda el `id` en SharedPreferences o tu almacenamiento local preferido.

---

## ?? Código Android (Kotlin)

### 1. Modelos de Datos

```kotlin
data class CreateUserDto(
    val email: String,
    val password: String,
    val phoneNumber: String? = null // OPCIONAL
)

data class LoginDto(
    val email: String,
    val password: String
)

data class UserDto(
    val id: String,
    val userName: String?,
    val email: String?,
    val phoneNumber: String?,
    val emailConfirmed: Boolean,
    val roles: List<String>
)
```

### 2. API Service

```kotlin
import io.ktor.client.*
import io.ktor.client.call.*
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import kotlinx.serialization.Serializable

class UserApiService(private val client: HttpClient) {
    
    private val baseUrl = "https://tu-api.com/api/Users"
    
    // Registrar usuario - SOLO email y contraseña
    suspend fun register(email: String, password: String): Result<UserDto> {
        return try {
            val response = client.post("$baseUrl/register") {
                contentType(ContentType.Application.Json)
                setBody(CreateUserDto(email, password))
            }
            
            if (response.status == HttpStatusCode.Created) {
                Result.success(response.body<UserDto>())
            } else {
                Result.failure(Exception("Error: ${response.status}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
    
    // Login - SOLO email y contraseña
    suspend fun login(email: String, password: String): Result<UserDto> {
        return try {
            val response = client.post("$baseUrl/login") {
                contentType(ContentType.Application.Json)
                setBody(LoginDto(email, password))
            }
            
            if (response.status == HttpStatusCode.OK) {
                Result.success(response.body<UserDto>())
            } else {
                Result.failure(Exception("Email o contraseña incorrectos"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }
}
```

### 3. ViewModel (Ejemplo con Jetpack Compose)

```kotlin
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

class AuthViewModel(
    private val userApiService: UserApiService,
    private val preferences: UserPreferences
) : ViewModel() {
    
    private val _uiState = MutableStateFlow<AuthUiState>(AuthUiState.Idle)
    val uiState: StateFlow<AuthUiState> = _uiState
    
    fun register(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            
            userApiService.register(email, password).fold(
                onSuccess = { user ->
                    // Guardar el userId
                    preferences.saveUserId(user.id)
                    _uiState.value = AuthUiState.Success(user)
                },
                onFailure = { error ->
                    _uiState.value = AuthUiState.Error(error.message ?: "Error desconocido")
                }
            )
        }
    }
    
    fun login(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState.Loading
            
            userApiService.login(email, password).fold(
                onSuccess = { user ->
                    // Guardar el userId
                    preferences.saveUserId(user.id)
                    _uiState.value = AuthUiState.Success(user)
                },
                onFailure = { error ->
                    _uiState.value = AuthUiState.Error(error.message ?: "Error desconocido")
                }
            )
        }
    }
}

sealed class AuthUiState {
    object Idle : AuthUiState()
    object Loading : AuthUiState()
    data class Success(val user: UserDto) : AuthUiState()
    data class Error(val message: String) : AuthUiState()
}
```

### 4. SharedPreferences Helper

```kotlin
import android.content.Context
import android.content.SharedPreferences

class UserPreferences(context: Context) {
    
    private val prefs: SharedPreferences = 
        context.getSharedPreferences("user_prefs", Context.MODE_PRIVATE)
    
    fun saveUserId(userId: String) {
        prefs.edit().putString("user_id", userId).apply()
    }
    
    fun getUserId(): String? {
        return prefs.getString("user_id", null)
    }
    
    fun clearUser() {
        prefs.edit().clear().apply()
    }
    
    fun isLoggedIn(): Boolean {
        return getUserId() != null
    }
}
```

### 5. UI - Login Screen (Jetpack Compose)

```kotlin
@Composable
fun LoginScreen(
    viewModel: AuthViewModel,
    onLoginSuccess: () -> Unit
) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    val uiState by viewModel.uiState.collectAsState()
    
    LaunchedEffect(uiState) {
        if (uiState is AuthUiState.Success) {
            onLoginSuccess()
        }
    }
    
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        // Email Input
        OutlinedTextField(
            value = email,
            onValueChange = { email = it },
            label = { Text("Email") },
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
            modifier = Modifier.fillMaxWidth()
        )
        
        Spacer(modifier = Modifier.height(8.dp))
        
        // Password Input
        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            label = { Text("Contraseña") },
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
            modifier = Modifier.fillMaxWidth()
        )
        
        Spacer(modifier = Modifier.height(16.dp))
        
        // Login Button
        Button(
            onClick = { viewModel.login(email, password) },
            enabled = uiState !is AuthUiState.Loading,
            modifier = Modifier.fillMaxWidth()
        ) {
            if (uiState is AuthUiState.Loading) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = Color.White
                )
            } else {
                Text("Iniciar Sesión")
            }
        }
        
        // Error Message
        if (uiState is AuthUiState.Error) {
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = (uiState as AuthUiState.Error).message,
                color = MaterialTheme.colorScheme.error
            )
        }
    }
}
```

---

## ?? Validaciones

### Email
- ? Debe ser un email válido
- ? No puede estar vacío
- ? Debe ser único (no puede estar registrado previamente)

### Contraseña
- ? Mínimo 6 caracteres
- ? No puede estar vacía
- ? Debe contener al menos una letra mayúscula
- ? Debe contener al menos un número
- ? Debe contener al menos un carácter especial

### PhoneNumber
- ? **OPCIONAL** - No es necesario enviarlo
- ? Si se envía, debe ser un número de teléfono válido

---

## ?? Flujo Recomendado

1. **Usuario abre la app**
   - Verificar si existe `userId` guardado en SharedPreferences
   - Si existe ? Ir a pantalla principal
   - Si no existe ? Mostrar pantalla de login/registro

2. **Usuario se registra**
   - Enviar solo email y contraseña a `/api/Users/register`
   - Guardar el `id` devuelto en SharedPreferences
   - Navegar a pantalla principal

3. **Usuario inicia sesión**
   - Enviar email y contraseña a `/api/Users/login`
   - Guardar el `id` devuelto en SharedPreferences
   - Navegar a pantalla principal

4. **Usuario cierra sesión**
   - Limpiar SharedPreferences
   - Navegar a pantalla de login

---

## ? Errores Comunes

### 400 Bad Request
- Email inválido
- Contraseña muy corta (menos de 6 caracteres)
- Email ya registrado (en registro)

### 401 Unauthorized
- Email o contraseña incorrectos (en login)

### 500 Internal Server Error
- Error del servidor
- Verificar logs del API

---

## ?? Probar el API

### Con cURL
```bash
# Registrar
curl -X POST https://tu-api.com/api/Users/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Login
curl -X POST https://tu-api.com/api/Users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```

### Con Postman
1. Crea una request POST a `https://tu-api.com/api/Users/register`
2. En Headers: `Content-Type: application/json`
3. En Body (raw, JSON):
```json
{
  "email": "test@example.com",
  "password": "Test123!"
}
```

---

## ?? ¿Necesitas agregar teléfono después?

Si más adelante quieres permitir que el usuario agregue su teléfono, usa el endpoint de actualización:

```http
PUT /api/Users/{id}
Content-Type: application/json

{
  "phoneNumber": "555-1234"
}
```

Nota: Este endpoint requiere autenticación.
