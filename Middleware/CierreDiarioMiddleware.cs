using ControlInventario.Models;
using ControlInventario.Services;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Middleware
{
    public class CierreDiarioMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public CierreDiarioMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Verificar si necesitamos cerrar el día anterior
            await VerificarYCierreDiaAnterior();

            await _next(context);
        }

        private async Task VerificarYCierreDiaAnterior()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ControlFarmaclinicContext>();
                var saldoService = scope.ServiceProvider.GetRequiredService<ISaldoCajaService>();

                // Obtener todas las cajas activas
                var cajasActivas = await context.Cajas
                    .Where(c => c.Activa == true)
                    .ToListAsync();

                var hoy = DateTime.Today;
                var diaAnterior = hoy.AddDays(-1);

                foreach (var caja in cajasActivas)
                {
                    // Verificar si el día anterior ya está cerrado
                    var yaCerrado = await saldoService.DiaCerradoAsync(caja.IdCaja, diaAnterior);

                    if (!yaCerrado)
                    {
                        // Verificar si hay movimientos en el día anterior
                        var movimientosDiaAnterior = await context.MovimientoCajas
                            .Where(m => m.IdCaja == caja.IdCaja && 
                                       m.Fecha.HasValue && 
                                       m.Fecha.Value.Date == diaAnterior.Date)
                            .AnyAsync();

                        if (movimientosDiaAnterior)
                        {
                            // Obtener un usuario para el cierre (el primer usuario activo)
                            var usuarioCierre = await context.Usuarios
                                .Where(u => u.Activo == true)
                                .FirstOrDefaultAsync();

                            if (usuarioCierre != null)
                            {
                                Console.WriteLine($"Cerrando automáticamente día {diaAnterior:yyyy-MM-dd} para caja {caja.Nombre}");
                                
                                // Cerrar el día
                                await saldoService.CerrarDiaAsync(caja.IdCaja, diaAnterior, usuarioCierre.IdUsuario);
                                
                                Console.WriteLine($"Día {diaAnterior:yyyy-MM-dd} cerrado exitosamente. Saldo final: {await saldoService.ObtenerSaldoFinalAsync(caja.IdCaja, diaAnterior)}");
                            }
                        }
                        else
                        {
                            // Si no hay movimientos, crear registro con saldo actual
                            var saldoInicial = await saldoService.ObtenerSaldoInicialAsync(caja.IdCaja, diaAnterior);
                            var usuarioCierre = await context.Usuarios
                                .Where(u => u.Activo == true)
                                .FirstOrDefaultAsync();

                            if (usuarioCierre != null)
                            {
                                await saldoService.GuardarSaldoDiarioAsync(
                                    caja.IdCaja, 
                                    diaAnterior, 
                                    saldoInicial, 
                                    saldoInicial, 
                                    0, 
                                    0, 
                                    usuarioCierre.IdUsuario
                                );

                                // Marcar como cerrado
                                await saldoService.CerrarDiaAsync(caja.IdCaja, diaAnterior, usuarioCierre.IdUsuario);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no detener la aplicación
                Console.WriteLine($"Error en middleware de cierre diario: {ex.Message}");
            }
        }
    }

    // Extension method para registrar el middleware
    public static class CierreDiarioMiddlewareExtensions
    {
        public static IApplicationBuilder UseCierreDiarioMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CierreDiarioMiddleware>();
        }
    }
}
