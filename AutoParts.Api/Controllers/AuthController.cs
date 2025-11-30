using AutoParts.Shared.Data;
using AutoParts.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AutoParts.Api.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Endpoint de login que retorna JWT Token
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for login attempt");
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: User not found - {Email}", loginDto.Email);
                return Unauthorized(new { message = "Email o contraseña incorrectos" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Generar JWT Token
                var jwtToken = GenerateJwtToken(user);
                var refreshToken = GenerateRefreshToken();
                
                var roles = await _userManager.GetRolesAsync(user);

                var loginResponse = new LoginResponse
                {
                    TokenType = "Bearer",
                    AccessToken = jwtToken,
                    ExpiresIn = 3600, // 1 hora
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        EmailConfirmed = user.EmailConfirmed,
                        Roles = roles.ToList()
                    }
                };

                _logger.LogInformation("User {Email} logged in successfully via /login endpoint", user.Email);
                _logger.LogInformation("JWT Token generated (first 30 chars): {TokenPrefix}...", 
                    jwtToken.Substring(0, Math.Min(30, jwtToken.Length)));

                return Ok(loginResponse);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out - {Email}", loginDto.Email);
                return BadRequest(new { message = "La cuenta está bloqueada. Intente más tarde." });
            }

            _logger.LogWarning("Login attempt failed: Invalid password - {Email}", loginDto.Email);
            return Unauthorized(new { message = "Email o contraseña incorrectos" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar sesión");
            return StatusCode(500, new { message = "Error al iniciar sesión", error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint de registro que retorna JWT Token
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "El email ya está registrado" });

            var user = new ApplicationUser
            {
                UserName = createUserDto.Email,
                Email = createUserDto.Email,
                PhoneNumber = createUserDto.PhoneNumber,
                EmailConfirmed = true // Auto-confirmar para app móvil
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Error al crear usuario: {errors}" });
            }

            // Asignar rol de User por defecto
            await _userManager.AddToRoleAsync(user, "User");

            // Generar JWT Token
            var jwtToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            
            var roles = await _userManager.GetRolesAsync(user);

            var loginResponse = new LoginResponse
            {
                TokenType = "Bearer",
                AccessToken = jwtToken,
                ExpiresIn = 3600,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList()
                }
            };

            _logger.LogInformation("Usuario {Email} registrado exitosamente via /register endpoint", user.Email);

            return CreatedAtAction(nameof(Login), loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar usuario");
            return StatusCode(500, new { message = "Error al registrar usuario" });
        }
    }

    /// <summary>
    /// Refresh token endpoint
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Aquí implementarías la lógica de refresh token
            // Por ahora, retornamos un error
            return BadRequest(new { message = "Refresh token no implementado aún" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al refrescar token");
            return StatusCode(500, new { message = "Error al refrescar token" });
        }
    }

    // Método privado para generar JWT Token
    private string GenerateJwtToken(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var jwtSecret = _configuration["Jwt:Secret"] 
            ?? Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? "AutoPartsSecretKeyForJwtTokenGeneration2024MustBeAtLeast32CharactersLong!";
        
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        // Agregar roles como claims
        var roles = _userManager.GetRolesAsync(user).Result;
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"] ?? "AutoPartsAPI",
            Audience = _configuration["Jwt:Audience"] ?? "AutoPartsApp",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        
        _logger.LogDebug("JWT Token length: {Length}", jwtToken.Length);
        _logger.LogDebug("JWT Token starts with: {Prefix}", jwtToken.Substring(0, Math.Min(20, jwtToken.Length)));
        
        return jwtToken;
    }

    // Método privado para generar Refresh Token
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
