using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Models;
using ControlInventario.Helpers;
using System.Threading.Tasks;

namespace ControlInventario.Controllers
{
    [AllowAnonymous]
    public class EstadisticasController : Controller
    {
        private readonly ControlFarmaclinicContext _context;

        public EstadisticasController(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        // GET: Estadisticas/GetEstadisticasRapidas
        [HttpGet]
        public async Task<JsonResult> GetEstadisticasRapidas()
        {
            var hoy = DateTimeHelper.GetClientDateTime().Date;
            var primerDiaMes = new DateTime(hoy.Year, hoy.Month, 1);

            Console.WriteLine($"Buscando estadísticas desde {primerDiaMes:yyyy-MM-dd} hasta {hoy:yyyy-MM-dd}");

            // Ventas del mes actual
            var ventasMes = await _context.Venta
                .Where(v => v.Fecha.HasValue && 
                           v.Fecha.Value >= primerDiaMes && 
                           v.Fecha.Value <= hoy && 
                           v.Anulada == false)
                .CountAsync();

            var montoMes = await _context.Venta
                .Where(v => v.Fecha.HasValue && 
                           v.Fecha.Value >= primerDiaMes && 
                           v.Fecha.Value <= hoy && 
                           v.Anulada == false)
                .SumAsync(v => v.Total);

            // Movimientos de caja del mes actual
            var movimientosCaja = await _context.MovimientoCajas
                .Where(m => m.Fecha.HasValue && 
                           m.Fecha.Value >= primerDiaMes && 
                           m.Fecha.Value <= hoy)
                .CountAsync();

            // Movimientos de inventario del mes actual
            var movimientosInventario = await _context.MovimientoInventarios
                .Where(m => m.Fecha.HasValue && 
                           m.Fecha.Value >= primerDiaMes && 
                           m.Fecha.Value <= hoy)
                .CountAsync();

            Console.WriteLine($"Resultados: Ventas={ventasMes}, Monto={montoMes}, Caja={movimientosCaja}, Inventario={movimientosInventario}");

            // Si no hay datos en el mes actual, buscar datos de todos los tiempos
            if (ventasMes == 0 && montoMes == 0 && movimientosCaja == 0 && movimientosInventario == 0)
            {
                Console.WriteLine("No hay datos en el mes actual, buscando datos históricos...");
                
                var ventasTotales = await _context.Venta
                    .Where(v => v.Anulada == false)
                    .CountAsync();

                var montoTotal = await _context.Venta
                    .Where(v => v.Anulada == false)
                    .SumAsync(v => v.Total);

                var movimientosCajaTotales = await _context.MovimientoCajas.CountAsync();
                var movimientosInventarioTotales = await _context.MovimientoInventarios.CountAsync();

                Console.WriteLine($"Totales históricos: Ventas={ventasTotales}, Monto={montoTotal}, Caja={movimientosCajaTotales}, Inventario={movimientosInventarioTotales}");

                return new JsonResult(new {
                    ventasMes = ventasTotales,
                    montoMes = montoTotal,
                    movimientosCaja = movimientosCajaTotales,
                    movimientosInventario = movimientosInventarioTotales
                });
            }

            return new JsonResult(new {
                ventasMes = ventasMes,
                montoMes = montoMes,
                movimientosCaja = movimientosCaja,
                movimientosInventario = movimientosInventario
            });
        }
    }
}
