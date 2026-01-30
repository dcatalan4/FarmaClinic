using ControlInventario.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Services
{
    public interface IMovimientoCajaService
    {
        Task<MovimientoCaja> CrearMovimientoAsync(int idCaja, string tipoMovimiento, decimal monto, 
            string concepto, int idUsuario, int? idReferencia = null);
        Task<decimal> ObtenerSaldoActualAsync(int idCaja);
        Task<List<MovimientoCaja>> ObtenerMovimientosPorPeriodoAsync(int idCaja, DateTime fechaInicio, DateTime fechaFin);
    }

    public class MovimientoCajaService : IMovimientoCajaService
    {
        private readonly ControlFarmaclinicContext _context;

        public MovimientoCajaService(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        public async Task<MovimientoCaja> CrearMovimientoAsync(int idCaja, string tipoMovimiento, decimal monto, 
            string concepto, int idUsuario, int? idReferencia = null)
        {
            // Obtener saldo actual de la caja
            var saldoActual = await ObtenerSaldoActualAsync(idCaja);

            // Calcular nuevo saldo
            var nuevoSaldo = tipoMovimiento.ToUpper() == "I" ? 
                saldoActual + monto : 
                saldoActual - monto;

            // Crear movimiento con el saldo en el momento
            var movimiento = new MovimientoCaja
            {
                IdCaja = idCaja,
                TipoMovimiento = tipoMovimiento.ToUpper(), // "I" para Ingreso, "E" para Egreso
                Monto = monto,
                Fecha = DateTime.Now,
                Concepto = concepto,
                IdUsuario = idUsuario,
                IdReferencia = idReferencia,
                SaldoEnMomento = nuevoSaldo
            };

            // Guardar movimiento
            _context.MovimientoCajas.Add(movimiento);

            // Actualizar saldo actual de la caja
            var caja = await _context.Cajas.FindAsync(idCaja);
            if (caja != null)
            {
                caja.SaldoActual = nuevoSaldo;
                _context.Update(caja);
            }

            await _context.SaveChangesAsync();

            Console.WriteLine($"Movimiento creado: {tipoMovimiento} {monto:C}, Saldo anterior: {saldoActual:C}, Nuevo saldo: {nuevoSaldo:C}");

            return movimiento;
        }

        public async Task<decimal> ObtenerSaldoActualAsync(int idCaja)
        {
            var caja = await _context.Cajas.FindAsync(idCaja);
            return caja?.SaldoActual ?? 0;
        }

        public async Task<List<MovimientoCaja>> ObtenerMovimientosPorPeriodoAsync(int idCaja, DateTime fechaInicio, DateTime fechaFin)
        {
            var inicio = fechaInicio.Date;
            var fin = fechaFin.Date.AddDays(1).AddTicks(-1);

            return await _context.MovimientoCajas
                .Include(m => m.IdCajaNavigation)
                .Include(m => m.IdUsuarioNavigation)
                .Where(m => m.IdCaja == idCaja && 
                           m.Fecha.HasValue && 
                           m.Fecha.Value >= inicio && 
                           m.Fecha.Value <= fin)
                .OrderBy(m => m.Fecha)
                .ToListAsync();
        }
    }
}
