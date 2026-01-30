using ControlInventario.Models;
using ControlInventario.Services;
using ControlInventario.Middleware;
using ControlInventario.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using System.Globalization;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory(),
    WebRootPath = "wwwroot"
});

// Configurar para evitar problemas de inotify en producción
builder.Host.ConfigureAppConfiguration((context, config) =>
{
    config.Sources.Clear();
    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);
    config.AddEnvironmentVariables();
});

// Configurar cultura para Guatemala (Quetzales)
var cultureInfo = new CultureInfo("es-GT");
cultureInfo.NumberFormat.CurrencySymbol = "Q";
cultureInfo.NumberFormat.CurrencyDecimalDigits = 2;
cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
cultureInfo.NumberFormat.CurrencyGroupSeparator = ",";

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews();

// Configurar autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
    });

// Configurar autorización
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("LectorPolicy", policy => 
        policy.RequireRole("Admin", "Lector"));
});

// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ControlFarmaclinicContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("ControlFarmaclinicContext"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
    });
    
    // Configurar DateTime para PostgreSQL
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
});

// Registrar servicios personalizados
builder.Services.AddScoped<ISaldoCajaService, SaldoCajaService>();
builder.Services.AddScoped<IMovimientoCajaService, MovimientoCajaService>();

var app = builder.Build();

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Middleware para cierre automático de días
app.UseCierreDiarioMiddleware();

app.UseRouting();

// Usar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
