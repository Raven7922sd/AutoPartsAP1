using AutoPartsAP1.Components;
using AutoPartsAP1.Components.Account;
using AutoPartsAP1.Components.Services; // Keep for CarritoService
using AutoParts.Shared.Data;
using AutoParts.Shared.Services;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar puerto dinámico solo en producción (Railway)
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
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
    client.BaseAddress = new Uri("https://autoparts-api.up.railway.app/");  // URL de tu API en Railway
});

builder.Services.AddBlazoredToast();
builder.Services.AddMudServices();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

var conStr = builder.Configuration.GetConnectionString("SqlServerConStr");
builder.Services.AddDbContextFactory<ApplicationDbContext>(o => o.UseSqlServer(conStr));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Aplicar migraciones automáticamente solo en producción
if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
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

// Usar HTTPS redirect solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
