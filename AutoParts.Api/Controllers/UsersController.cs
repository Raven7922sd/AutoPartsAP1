using AutoParts.Shared.Data;
using AutoParts.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    // GET: api/Users
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        try
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    Roles = roles.ToList()
                });
            }

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuarios");
            return StatusCode(500, "Error al obtener usuarios");
        }
    }

    // GET: api/Users/{id}
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Usuario con ID {id} no encontrado");

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles.ToList()
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario");
            return StatusCode(500, "Error al obtener usuario");
        }
    }

    // POST: api/Users/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingUser != null)
                return BadRequest("El email ya está registrado");

            var user = new ApplicationUser
            {
                UserName = createUserDto.Email,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.PhoneNumber, // Opcional, puede ser null
                EmailConfirmed = true // Auto-confirmar para app móvil
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"Error al crear usuario: {errors}");
            }

            // Asignar rol de User por defecto
            await _userManager.AddToRoleAsync(user, "User");

            _logger.LogInformation("Usuario {Email} registrado exitosamente", user.Email);

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar usuario");
            return StatusCode(500, "Error al registrar usuario");
        }
    }

    // POST: api/Users/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
                return Unauthorized("Email o contraseña incorrectos");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userDto = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList()
                };

                _logger.LogInformation("Usuario {Email} inició sesión exitosamente", user.Email);
                return Ok(userDto);
            }

            if (result.IsLockedOut)
            {
                return BadRequest("La cuenta está bloqueada. Intente más tarde.");
            }

            return Unauthorized("Email o contraseña incorrectos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar sesión");
            return StatusCode(500, "Error al iniciar sesión");
        }
    }

    // PUT: api/Users/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> UpdateUser(string id, [FromBody] UpdateUserDto updateUserDto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Usuario con ID {id} no encontrado");

            // Verificar que el usuario solo pueda actualizar su propia información (excepto Admin)
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            
            if (currentUserId != id && !isAdmin)
                return Forbid();

            // Actualizar email
            if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, updateUserDto.Email);
                if (!setEmailResult.Succeeded)
                    return BadRequest("Error al actualizar email");
                
                await _userManager.SetUserNameAsync(user, updateUserDto.Email);
            }

            // Actualizar teléfono
            if (updateUserDto.PhoneNumber != null)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, updateUserDto.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                    return BadRequest("Error al actualizar teléfono");
            }

            // Cambiar contraseña
            if (!string.IsNullOrEmpty(updateUserDto.CurrentPassword) && !string.IsNullOrEmpty(updateUserDto.NewPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, updateUserDto.CurrentPassword, updateUserDto.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                    return BadRequest($"Error al cambiar contraseña: {errors}");
                }
            }

            _logger.LogInformation("Usuario {Email} actualizado exitosamente", user.Email);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar usuario");
            return StatusCode(500, "Error al actualizar usuario");
        }
    }

    // DELETE: api/Users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound($"Usuario con ID {id} no encontrado");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest($"Error al eliminar usuario: {errors}");
            }

            _logger.LogInformation("Usuario {Email} eliminado exitosamente", user.Email);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar usuario");
            return StatusCode(500, "Error al eliminar usuario");
        }
    }

    // GET: api/Users/email/{email}
    [HttpGet("email/{email}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound($"Usuario con email {email} no encontrado");

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                Roles = roles.ToList()
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener usuario por email");
            return StatusCode(500, "Error al obtener usuario");
        }
    }

    // GET: api/Users/count - Endpoint público para verificar conexión
    [HttpGet("count")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetUsersCount()
    {
        try
        {
            var count = await _userManager.Users.CountAsync();
            return Ok(new { 
                totalUsers = count,
                message = "API funcionando correctamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al contar usuarios");
            return StatusCode(500, "Error al contar usuarios");
        }
    }

    // GET: api/Users/test - Endpoint de prueba público
    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult<object> Test()
    {
        return Ok(new
        {
            status = "OK",
            message = "Users API está funcionando correctamente",
            timestamp = DateTime.UtcNow,
            endpoints = new
            {
                register = "POST /api/Users/register",
                login = "POST /api/Users/login",
                getUsers = "GET /api/Users (requiere Admin)",
                getUser = "GET /api/Users/{id} (requiere autenticación)",
                count = "GET /api/Users/count (público)"
            }
        });
    }
}
