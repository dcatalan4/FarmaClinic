using ControlInventario.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Services
{
    public interface ISaldoCajaService
    {
        Task<decimal> ObtenerSaldoInicialAsync(int idCaja, DateTime fecha);
        Task<decimal> ObtenerSaldoFinalAsync(int idCaja, DateTime fecha);
        Task<decimal> ObtenerSaldoActualAsync(int idCaja);
        Task GuardarSaldoDiarioAsync(int idCaja, DateTime fecha, decimal saldoInicial, decimal saldoFinal, 
            decimal totalIngresos, decimal totalEgresos, int idUsuario);
        Task CerrarDiaAsync(int idCaja, DateTime fecha, int idUsuario);
        Task<bool> DiaCerradoAsync(int idCaja, DateTime fecha);
    }

    public class SaldoCajaService : ISaldoCajaService
    {
        private readonly ControlFarmaclinicContext _context;

        public SaldoCajaService(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        public async Task<decimal> ObtenerSaldoInicialAsync(int idCaja, DateTime fecha)
        {
            // Buscar saldo final del día anterior
            var diaAnterior = fecha.AddDays(-1);
            var saldoAnterior = await _context.SaldoCajaDiarios
                .Where(s => s.IdCaja == idCaja && s.Fecha == diaAnterior && s.Cerrado)
                .FirstOrDefaultAsync();

            if (saldoAnterior != null)
            {
                return saldoAnterior.SaldoFinal;
            }

            // Si no hay registro anterior, buscar el saldo actual de la caja
            var caja = await _context.Cajas.FindAsync(idCaja);
            return caja?.SaldoActual ?? 0;
        }

        public async Task<decimal> ObtenerSaldoFinalAsync(int idCaja, DateTime fecha)
        {
            // Obtener saldo inicial
            var saldoInicial = await ObtenerSaldoInicialAsync(idCaja, fecha);

            // Calcular movimientos del día
            var movimientosDia = await _context.MovimientoCajas
                .Where(m => m.IdCaja == idCaja && 
                           m.Fecha.HasValue && 
                           m.Fecha.Value.Date == fecha.Date)
                .ToListAsync();

            var totalIngresos = movimientosDia.Where(m => m.TipoMovimiento == "I").Sum(m => m.Monto);
            var totalEgresos = movimientosDia.Where(m => m.TipoMovimiento == "E").Sum(m => m.Monto);

            return saldoInicial + totalIngresos - totalEgresos;
        }

        public async Task<decimal> ObtenerSaldoActualAsync(int idCaja)
        {
            var caja = await _context.Cajas.FindAsync(idCaja);
            return caja?.SaldoActual ?? 0;
        }

        public async Task GuardarSaldoDiarioAsync(int idCaja, DateTime fecha, decimal saldoInicial, 
            decimal saldoFinal, decimal totalIngresos, decimal totalEgresos, int idUsuario)
        {
            var saldoDiario = await _context.SaldoCajaDiarios
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
                    Cerrado = false
                };
                _context.SaldoCajaDiarios.Add(saldoDiario);
            }
            else
            {
                saldoDiario.SaldoInicial = saldoInicial;
                saldoDiario.SaldoFinal = saldoFinal;
                saldoDiario.TotalIngresos = totalIngresos;
                saldoDiario.TotalEgresos = totalEgresos;
                saldoDiario.IdUsuarioCierre = idUsuario;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CerrarDiaAsync(int idCaja, DateTime fecha, int idUsuario)
        {
            var saldoFinal = await ObtenerSaldoFinalAsync(idCaja, fecha);
            var saldoInicial = await ObtenerSaldoInicialAsync(idCaja, fecha);

            var movimientosDia = await _context.MovimientoCajas
                .Where(m => m.IdCaja == idCaja && 
                           m.Fecha.HasValue && 
                           m.Fecha.Value.Date == fecha.Date)
                .ToListAsync();

            var totalIngresos = movimientosDia.Where(m => m.TipoMovimiento == "I").Sum(m => m.Monto);
            var totalEgresos = movimientosDia.Where(m => m.TipoMovimiento == "E").Sum(m => m.Monto);

            await GuardarSaldoDiarioAsync(idCaja, fecha, saldoInicial, saldoFinal, 
                totalIngresos, totalEgresos, idUsuario);

            // Marcar como cerrado
            var saldoDiario = await _context.SaldoCajaDiarios
                .FirstOrDefaultAsync(s => s.IdCaja == idCaja && s.Fecha == fecha);
            
            if (saldoDiario != null)
            {
                saldoDiario.Cerrado = true;
                saldoDiario.FechaCierre = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            // Actualizar saldo actual de la caja
            var caja = await _context.Cajas.FindAsync(idCaja);
            if (caja != null)
            {
                caja.SaldoActual = saldoFinal;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> DiaCerradoAsync(int idCaja, DateTime fecha)
        {
            var saldoDiario = await _context.SaldoCajaDiarios
                .FirstOrDefaultAsync(s => s.IdCaja == idCaja && s.Fecha == fecha);
            
            return saldoDiario?.Cerrado ?? false;
        }
    }
}
