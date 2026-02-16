using ControlInventario.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Services
{
    public class CierreDiarioBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CierreDiarioBackgroundService> _logger;
        private static DateTime _ultimaEjecucion = DateTime.MinValue;
        private static readonly object _lock = new object();

        public CierreDiarioBackgroundService(IServiceProvider serviceProvider, ILogger<CierreDiarioBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Ejecutar cada 30 minutos
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                    
                    if (DebeEjecutarCierre())
                    {
                        _logger.LogInformation("Iniciando proceso de cierre diario automático");
                        await EjecutarCierreDiario();
                        _logger.LogInformation("Proceso de cierre diario completado");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cuando se detiene el servicio
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el servicio de cierre diario");
                }
            }
        }

        private bool DebeEjecutarCierre()
        {
            lock (_lock)
            {
                var ahora = DateTime.Now;
                // Solo ejecutar una vez al día, a las 2:00 AM
                if (ahora.Date > _ultimaEjecucion.Date && ahora.Hour >= 2)
                {
                    _ultimaEjecucion = ahora;
                    return true;
                }
                return false;
            }
        }

        private async Task EjecutarCierreDiario()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ControlFarmaclinicContext>();

            var hoy = DateTime.Today;
            var diaAnterior = hoy.AddDays(-1);

            // Verificar si ya existe un registro de cierre para hoy
            var yaProcesadoHoy = await context.SaldoCajaDiarios
                .AnyAsync(s => s.Fecha >= hoy && s.Cerrado);

            if (yaProcesadoHoy)
            {
                _logger.LogInformation("Cierre diario ya procesado hoy, omitiendo...");
                return;
            }

            // Obtener datos en consultas optimizadas
            var cajasActivas = await context.Cajas
                .Where(c => c.Activa == true)
                .Select(c => new { c.IdCaja, c.Nombre, c.SaldoActual })
                .ToListAsync();

            if (!cajasActivas.Any())
            {
                _logger.LogInformation("No hay cajas activas para procesar");
                return;
            }

            // Obtener todos los datos necesarios en una sola consulta
            var idsCajas = cajasActivas.Select(c => c.IdCaja).ToList();
            
            var saldosExistentes = await context.SaldoCajaDiarios
                .Where(s => idsCajas.Contains(s.IdCaja) && s.Fecha == diaAnterior)
                .ToDictionaryAsync(s => s.IdCaja, s => s);

            var movimientosAgrupados = await context.MovimientoCajas
                .Where(m => idsCajas.Contains(m.IdCaja) && 
                           m.Fecha.HasValue && 
                           m.Fecha.Value.Date == diaAnterior.Date)
                .GroupBy(m => m.IdCaja)
                .Select(g => new 
                {
                    IdCaja = g.Key,
                    TieneMovimientos = g.Any(),
                    TotalIngresos = g.Where(m => m.TipoMovimiento == "I").Sum(m => m.Monto),
                    TotalEgresos = g.Where(m => m.TipoMovimiento == "E").Sum(m => m.Monto)
                })
                .ToListAsync();

            var movimientosDict = movimientosAgrupados.ToDictionary(x => x.IdCaja, x => (object)x);

            var usuarioCierre = await context.Usuarios
                .Where(u => u.Activo == true)
                .Select(u => new { u.IdUsuario })
                .FirstOrDefaultAsync();

            if (usuarioCierre == null)
            {
                _logger.LogWarning("No hay usuarios activos para el cierre");
                return;
            }

            // Procesar cada caja
            foreach (var caja in cajasActivas)
            {
                await ProcesarCajaIndividual(context, caja, diaAnterior, saldosExistentes, movimientosDict, usuarioCierre.IdUsuario);
            }

            _logger.LogInformation($"Proceso de cierre diario completado para {cajasActivas.Count} cajas");
        }

        private async Task ProcesarCajaIndividual(ControlFarmaclinicContext context, dynamic caja, DateTime diaAnterior, 
            Dictionary<int, SaldoCajaDiario> saldosExistentes, Dictionary<int, object> movimientosAgrupados, int idUsuarioCierre)
        {
            // Verificar si ya está cerrado
            if (saldosExistentes.TryGetValue(caja.IdCaja, out SaldoCajaDiario saldoExistente) && saldoExistente.Cerrado)
            {
                _logger.LogInformation($"Caja {caja.Nombre} - Día {diaAnterior:yyyy-MM-dd} ya está cerrado");
                return;
            }

            var tieneMovimientos = movimientosAgrupados.TryGetValue(caja.IdCaja, out object movObj) && movObj != null;
            
            if (tieneMovimientos)
            {
                dynamic mov = movObj;
                _logger.LogInformation($"Cerrando día {diaAnterior:yyyy-MM-dd} para caja {caja.Nombre} (con movimientos)");
                
                var saldoInicial = await ObtenerSaldoInicial(context, caja.IdCaja, diaAnterior, caja.SaldoActual);
                var totalIngresos = (decimal)mov.TotalIngresos;
                var totalEgresos = (decimal)mov.TotalEgresos;
                var saldoFinal = saldoInicial + totalIngresos - totalEgresos;
                
                await GuardarSaldoDiario(context, caja.IdCaja, diaAnterior, saldoInicial, saldoFinal, 
                    totalIngresos, totalEgresos, idUsuarioCierre, true);
                
                await ActualizarSaldoCaja(context, caja.IdCaja, saldoFinal);
                
                _logger.LogInformation($"Día {diaAnterior:yyyy-MM-dd} cerrado para caja {caja.Nombre}. Saldo final: {saldoFinal}");
            }
            else
            {
                _logger.LogInformation($"Caja {caja.Nombre} - No hay movimientos para el día {diaAnterior:yyyy-MM-dd}");
                
                var saldoInicial = await ObtenerSaldoInicial(context, caja.IdCaja, diaAnterior, caja.SaldoActual);
                
                await GuardarSaldoDiario(context, caja.IdCaja, diaAnterior, saldoInicial, saldoInicial, 
                    0, 0, idUsuarioCierre, true);
                
                await ActualizarSaldoCaja(context, caja.IdCaja, saldoInicial);
            }
        }

        private async Task<decimal> ObtenerSaldoInicial(ControlFarmaclinicContext context, int idCaja, DateTime fecha, decimal saldoActual)
        {
            var diaAnterior = fecha.AddDays(-1);
            var saldoAnterior = await context.SaldoCajaDiarios
                .Where(s => s.IdCaja == idCaja && s.Fecha == diaAnterior && s.Cerrado)
                .Select(s => s.SaldoFinal)
                .FirstOrDefaultAsync();

            return saldoAnterior > 0 ? saldoAnterior : saldoActual;
        }

        private async Task GuardarSaldoDiario(ControlFarmaclinicContext context, int idCaja, DateTime fecha, 
            decimal saldoInicial, decimal saldoFinal, decimal totalIngresos, decimal totalEgresos, int idUsuario, bool cerrado)
        {
            var saldoDiario = await context.SaldoCajaDiarios
                .FirstOrDefaultAsync(s => s.IdCaja == idCaja && s.Fecha == fecha);

            if (saldoDiario == null)
            {
                saldoDiario = new SaldoCajaDiario
                {
                    IdCaja = idCaja,
                    Fecha = fecha,
                    SaldoInicial = saldoInicial,
                    SaldoFinal = saldoFinal,
                    TotalIngresos = totalIngresos,
                    TotalEgresos = totalEgresos,
                    IdUsuarioCierre = idUsuario,
                    Cerrado = cerrado,
                    FechaCierre = cerrado ? DateTime.Now : (DateTime?)null
                };
                context.SaldoCajaDiarios.Add(saldoDiario);
            }
            else
            {
                saldoDiario.SaldoInicial = saldoInicial;
                saldoDiario.SaldoFinal = saldoFinal;
                saldoDiario.TotalIngresos = totalIngresos;
                saldoDiario.TotalEgresos = totalEgresos;
                saldoDiario.IdUsuarioCierre = idUsuario;
                saldoDiario.Cerrado = cerrado;
                saldoDiario.FechaCierre = cerrado ? DateTime.Now : saldoDiario.FechaCierre;
            }

            await context.SaveChangesAsync();
        }

        private async Task ActualizarSaldoCaja(ControlFarmaclinicContext context, int idCaja, decimal nuevoSaldo)
        {
            await context.Cajas
                .Where(c => c.IdCaja == idCaja)
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.SaldoActual, nuevoSaldo));
        }
    }
}
