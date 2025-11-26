using AutoPartsAP1.Components;
using AutoPartsAP1.Components.Account;
using AutoPartsAP1.Components.Services;
using AutoParts.Shared.Data;
using AutoParts.Shared.Services;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar Kestrel para Railway
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        serverOptions.ListenAnyIP(int.Parse(port));
    });
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Shared services
builder.Services.AddScoped<AutoParts.Shared.Services.ProductoService>();
builder.Services.AddScoped<AutoParts.Shared.Services.VentasService>();
builder.Services.AddScoped<AutoParts.Shared.Services.ComprasService>();
builder.Services.AddScoped<AutoParts.Shared.Services.ServiciosService>();
builder.Services.AddScoped<AutoParts.Shared.Services.CitaService>();

// Local Blazor-specific service
builder.Services.AddScoped<AutoPartsAP1.Components.Services.CarritoService>();

// HttpClient para consumir API (opcional)
builder.Services.AddHttpClient("AutoPartsApi", client =>
{
    var apiUrl = Environment.GetEnvironmentVariable("API_URL") ?? "https://autoparts-api.up.railway.app/";
    client.BaseAddress = new Uri(apiUrl);
});

builder.Services.AddBlazoredToast();
builder.Services.AddMudServices();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

var conStr = builder.Configuration.GetConnectionString("SqlServerConStr")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__SqlServerConStr");

if (string.IsNullOrEmpty(conStr))
{
    throw new InvalidOperationException("Connection string 'SqlServerConStr' not found.");
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(o => o.UseSqlServer(conStr));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Aplicar migraciones automáticamente en producción
if (!app.Environment.IsDevelopment())
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            app.Logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations applied successfully.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating the database.");
        // No lanzar excepción para permitir que la app inicie
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// HTTPS redirect solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 1. Archivos estáticos primero
app.UseStaticFiles();

// 2. Routing
app.UseRouting();

// 3. Authentication (debe ir ANTES de Authorization y Antiforgery)
app.UseAuthentication();

// 4. Authorization (debe ir DESPUÉS de Authentication y ANTES de Antiforgery)
app.UseAuthorization();

// 5. Antiforgery (debe ir DESPUÉS de Authentication y Authorization)
app.UseAntiforgery();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    version = "1.0.0"
})).AllowAnonymous();

// MapStaticAssets para assets optimizados
app.MapStaticAssets();

// Mapear componentes Razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Identity endpoints
app.MapAdditionalIdentityEndpoints();

app.Logger.LogInformation("Application started successfully on {Environment}", app.Environment.EnvironmentName);

app.Run();
