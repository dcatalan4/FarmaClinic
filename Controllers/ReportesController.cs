using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Models;
using ControlInventario.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ControlInventario.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportesController : Controller
    {
        private readonly ControlFarmaclinicContext _context;
        private readonly ISaldoCajaService _saldoCajaService;
        private readonly IMovimientoCajaService _movimientoCajaService;

        public ReportesController(ControlFarmaclinicContext context, ISaldoCajaService saldoCajaService, 
            IMovimientoCajaService movimientoCajaService)
        {
            _context = context;
            _saldoCajaService = saldoCajaService;
            _movimientoCajaService = movimientoCajaService;
        }

        // GET: Reportes
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reportes/Ventas
        public IActionResult Ventas()
        {
            var hoy = DateTime.Today;
            var primerDiaMes = new DateTime(hoy.Year, hoy.Month, 1);
            
            var model = new VentasReporteViewModel
            {
                FechaInicio = primerDiaMes,
                FechaFin = hoy,
                TipoReporte = "completo",
                Ventas = new List<Ventum>(),
                TotalVentas = 0,
                MontoTotal = 0,
                TicketPromedio = 0,
                ResumenPorVendedor = new List<ResumenVendedor>(),
                ResumenPorDia = new List<ResumenDiario>(),
                TopProductos = new List<TopProducto>()
            };
            
            return View(model);
        }

        // POST: Reportes/Ventas
        [HttpPost]
        public async Task<IActionResult> Ventas(DateTime fechaInicio, DateTime fechaFin)
        {
            var ventasQuery = _context.Venta
                .Include(v => v.IdUsuarioNavigation)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(dv => dv.IdProductoNavigation)
                .Where(v => v.Fecha.HasValue && 
                           v.Fecha.Value >= fechaInicio.Date && 
                           v.Fecha.Value <= fechaFin.Date.AddDays(1).AddTicks(-1));

            var ventas = await ventasQuery
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            var reporte = new VentasReporteViewModel
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TipoReporte = "detallado",
                Ventas = ventas,
                TotalVentas = ventas.Where(v => v.Anulada != true).Count(),
                MontoTotal = ventas.Where(v => v.Anulada != true).Sum(v => v.Total),
                TicketPromedio = ventas.Where(v => v.Anulada != true).Any() ? ventas.Where(v => v.Anulada != true).Average(v => v.Total) : 0
            };

            // Resumen por vendedor (solo ventas activas)
            reporte.ResumenPorVendedor = ventas
                .Where(v => v.Anulada != true)
                .GroupBy(v => new { v.IdUsuario, v.IdUsuarioNavigation.Nombre })
                .Select(g => new ResumenVendedor
                {
                    Vendedor = g.Key.Nombre,
                    CantidadVentas = g.Count(),
                    MontoTotal = g.Sum(v => v.Total),
                    TicketPromedio = g.Any() ? g.Average(v => v.Total) : 0
                })
                .OrderByDescending(r => r.MontoTotal)
                .ToList();

            // Resumen por día (solo ventas activas)
            reporte.ResumenPorDia = ventas
                .Where(v => v.Anulada != true)
                .GroupBy(v => v.Fecha.Value.Date)
                .Select(g => new ResumenDiario
                {
                    Fecha = g.Key,
                    CantidadVentas = g.Count(),
                    MontoTotal = g.Sum(v => v.Total),
                    TicketPromedio = g.Any() ? g.Average(v => v.Total) : 0
                })
                .OrderBy(r => r.Fecha)
                .ToList();

            // Top productos vendidos (solo ventas activas)
            reporte.TopProductos = ventas
                .Where(v => v.Anulada != true)
                .SelectMany(v => v.DetalleVenta)
                .GroupBy(dv => new { dv.IdProducto, dv.IdProductoNavigation.Nombre })
                .Select(g => new TopProducto
                {
                    Producto = g.Key.Nombre,
                    CantidadTotal = g.Sum(dv => dv.Cantidad),
                    MontoTotal = g.Sum(dv => dv.Subtotal)
                })
                .OrderByDescending(p => p.CantidadTotal)
                .Take(10)
                .ToList();

            return View(reporte);
        }

        // GET: Reportes/Caja
        public IActionResult Caja()
        {
            var hoy = DateTime.Today;
            var primerDiaMes = new DateTime(hoy.Year, hoy.Month, 1);
            
            var model = new CajaReporteViewModel
            {
                FechaInicio = primerDiaMes,
                FechaFin = hoy,
                Movimientos = new List<MovimientoCaja>(),
                TotalMovimientos = 0,
                SaldoInicial = 0,
                SaldoFinal = 0,
                ResumenPorTipo = new List<ResumenTipoMovimiento>(),
                ResumenPorDia = new List<ResumenCajaDiario>()
            };
            
            return View(model);
        }

        // POST: Reportes/Caja
        [HttpPost]
        public async Task<IActionResult> Caja(DateTime fechaInicio, DateTime fechaFin)
        {
            Console.WriteLine($"=== INICIO REPORTE CAJA ===");
            Console.WriteLine($"Fecha Inicio: {fechaInicio:yyyy-MM-dd}");
            Console.WriteLine($"Fecha Fin: {fechaFin:yyyy-MM-dd}");
            
            // Obtener caja principal
            var cajaPrincipal = await _context.Cajas
                .Where(c => c.Activa == true)
                .FirstOrDefaultAsync();

            if (cajaPrincipal == null)
            {
                Console.WriteLine("ERROR: No hay caja principal activa");
                return View(new CajaReporteViewModel
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Movimientos = new List<MovimientoCaja>(),
                    TotalMovimientos = 0,
                    SaldoInicial = 0,
                    SaldoFinal = 0
                });
            }

            Console.WriteLine($"Caja encontrada: ID={cajaPrincipal.IdCaja}, SaldoActual={cajaPrincipal.SaldoActual}");

            // Obtener movimientos del período usando el nuevo servicio
            var movimientos = await _movimientoCajaService.ObtenerMovimientosPorPeriodoAsync(
                cajaPrincipal.IdCaja, fechaInicio, fechaFin);

            Console.WriteLine($"Se encontraron {movimientos.Count} movimientos en el período");

            // Mostrar detalles de movimientos
            foreach (var mov in movimientos.Take(5))
            {
                Console.WriteLine($"  Movimiento: {mov.Fecha:yyyy-MM-dd HH:mm} | {mov.TipoMovimiento} | {mov.Monto:C} | Saldo: {mov.SaldoEnMomento:C}");
            }

            // Obtener saldo inicial (primer movimiento del período o saldo actual de caja si no hay movimientos)
            decimal saldoInicial = 0;
            if (movimientos.Any())
            {
                // Buscar el último movimiento antes del período para obtener el saldo inicial
                var ultimoMovimientoAnterior = await _context.MovimientoCajas
                    .Where(m => m.IdCaja == cajaPrincipal.IdCaja && 
                               m.Fecha.HasValue && 
                               m.Fecha.Value < fechaInicio.Date)
                    .OrderByDescending(m => m.Fecha)
                    .FirstOrDefaultAsync();

                saldoInicial = ultimoMovimientoAnterior?.SaldoEnMomento ?? 0;
                Console.WriteLine($"Saldo inicial calculado desde movimiento anterior: {saldoInicial:C}");
            }
            else
            {
                // Si no hay movimientos en el período, usar el saldo actual de la caja
                saldoInicial = await _movimientoCajaService.ObtenerSaldoActualAsync(cajaPrincipal.IdCaja);
                Console.WriteLine($"Saldo inicial desde saldo actual de caja: {saldoInicial:C}");
            }

            // Obtener saldo final (último movimiento del período o saldo actual si no hay movimientos)
            decimal saldoFinal = 0;
            if (movimientos.Any())
            {
                saldoFinal = movimientos.Last().SaldoEnMomento;
                Console.WriteLine($"Saldo final desde último movimiento: {saldoFinal:C}");
            }
            else
            {
                saldoFinal = await _movimientoCajaService.ObtenerSaldoActualAsync(cajaPrincipal.IdCaja);
                Console.WriteLine($"Saldo final desde saldo actual de caja: {saldoFinal:C}");
            }

            Console.WriteLine($"Cálculo de saldos:");
            Console.WriteLine($"  Saldo Inicial: {saldoInicial:C}");
            Console.WriteLine($"  Saldo Final: {saldoFinal:C}");
            Console.WriteLine($"  Movimientos en período: {movimientos.Count}");
            Console.WriteLine($"  Variación Neta: {(saldoFinal - saldoInicial):C}");

            var reporte = new CajaReporteViewModel
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Movimientos = movimientos,
                TotalMovimientos = movimientos.Count,
                SaldoInicial = saldoInicial,
                SaldoFinal = saldoFinal
            };

            Console.WriteLine($"Reporte generado: {movimientos.Count} movimientos, Saldo Inicial: {saldoInicial:C}, Saldo Final: {reporte.SaldoFinal:C}");

            // Resumen por tipo de movimiento
            reporte.ResumenPorTipo = movimientos
                .GroupBy(m => m.TipoMovimiento)
                .Select(g => new ResumenTipoMovimiento
                {
                    Tipo = g.Key,
                    Cantidad = g.Count(),
                    MontoTotal = g.Sum(m => m.TipoMovimiento == "I" ? m.Monto : -m.Monto)
                })
                .ToList();

            // Resumen por día
            reporte.ResumenPorDia = movimientos
                .GroupBy(m => m.Fecha.Value.Date)
                .Select(g => new ResumenCajaDiario
                {
                    Fecha = g.Key,
                    Ingresos = g.Where(m => m.TipoMovimiento == "I").Sum(m => m.Monto),
                    Egresos = g.Where(m => m.TipoMovimiento == "E").Sum(m => m.Monto),
                    Neto = g.Where(m => m.TipoMovimiento == "I").Sum(m => m.Monto) - g.Where(m => m.TipoMovimiento == "E").Sum(m => m.Monto)
                })
                .OrderBy(r => r.Fecha)
                .ToList();

            Console.WriteLine($"Retornando vista con reporte de caja");
            return View(reporte);
        }

        // GET: Reportes/Inventario
        public IActionResult Inventario()
        {
            var hoy = DateTime.Today;
            var primerDiaMes = new DateTime(hoy.Year, hoy.Month, 1);
            
            var model = new InventarioReporteViewModel
            {
                FechaInicio = primerDiaMes,
                FechaFin = hoy,
                TipoReporte = "completo",
                Movimientos = new List<MovimientoInventario>(),
                TotalMovimientos = 0,
                ResumenPorTipo = new List<ResumenInventarioTipo>(),
                ResumenPorProducto = new List<ResumenProductoInventario>(),
                ResumenPorDia = new List<ResumenInventarioDiario>(),
                TopProductosMovimiento = new List<TopProductoMovimiento>()
            };
            
            return View(model);
        }

        // POST: Reportes/Inventario
        [HttpPost]
        public async Task<IActionResult> Inventario(DateTime fechaInicio, DateTime fechaFin)
        {
            var movimientosQuery = _context.MovimientoInventarios
                .Include(m => m.IdProductoNavigation)
                .Where(m => m.Fecha.HasValue && 
                           m.Fecha.Value >= fechaInicio.Date && 
                           m.Fecha.Value <= fechaFin.Date.AddDays(1).AddTicks(-1));

            var movimientos = await movimientosQuery
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            var reporte = new InventarioReporteViewModel
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TipoReporte = "detallado",
                Movimientos = movimientos,
                TotalMovimientos = movimientos.Count
            };

            // Resumen por tipo de movimiento (corregido para mostrar correctamente)
            reporte.ResumenPorTipo = movimientos
                .GroupBy(m => m.TipoMovimiento)
                .Select(g => new ResumenInventarioTipo
                {
                    Tipo = g.Key == "E" ? "Entrada" : "Salida",
                    Cantidad = g.Sum(m => m.Cantidad),
                    ProductosAfectados = g.Select(m => m.IdProducto).Distinct().Count()
                })
                .ToList();

            // Resumen por producto
            reporte.ResumenPorProducto = movimientos
                .GroupBy(m => new { m.IdProducto, m.IdProductoNavigation.Nombre })
                .Select(g => new ResumenProductoInventario
                {
                    Producto = g.Key.Nombre,
                    Ingresos = g.Where(m => m.TipoMovimiento == "E").Sum(m => m.Cantidad),
                    Egresos = g.Where(m => m.TipoMovimiento == "S").Sum(m => m.Cantidad),
                    Neto = g.Where(m => m.TipoMovimiento == "E").Sum(m => m.Cantidad) - g.Where(m => m.TipoMovimiento == "S").Sum(m => m.Cantidad)
                })
                .OrderByDescending(r => Math.Abs(r.Neto))
                .Take(20)
                .ToList();

            // Resumen por día
            reporte.ResumenPorDia = movimientos
                .GroupBy(m => m.Fecha.Value.Date)
                .Select(g => new ResumenInventarioDiario
                {
                    Fecha = g.Key,
                    Ingresos = g.Where(m => m.TipoMovimiento == "E").Sum(m => m.Cantidad),
                    Egresos = g.Where(m => m.TipoMovimiento == "S").Sum(m => m.Cantidad),
                    Neto = g.Where(m => m.TipoMovimiento == "E").Sum(m => m.Cantidad) - g.Where(m => m.TipoMovimiento == "S").Sum(m => m.Cantidad)
                })
                .OrderBy(r => r.Fecha)
                .ToList();

            // Productos con mayor movimiento
            reporte.TopProductosMovimiento = movimientos
                .GroupBy(m => new { m.IdProducto, m.IdProductoNavigation.Nombre })
                .Select(g => new TopProductoMovimiento
                {
                    Producto = g.Key.Nombre,
                    TotalMovimientos = g.Count(),
                    CantidadTotal = g.Sum(m => m.Cantidad)
                })
                .OrderByDescending(p => p.CantidadTotal)
                .Take(10)
                .ToList();

            return View(reporte);
        }

        // GET: Reportes/GetEstadisticasRapidas
        [HttpGet]
        public async Task<IActionResult> GetEstadisticasRapidas()
        {
            var hoy = DateTime.Today;
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

                return Json(new {
                    ventasMes = ventasTotales,
                    montoMes = montoTotal,
                    movimientosCaja = movimientosCajaTotales,
                    movimientosInventario = movimientosInventarioTotales
                });
            }

            return Json(new {
                ventasMes = ventasMes,
                montoMes = montoMes,
                movimientosCaja = movimientosCaja,
                movimientosInventario = movimientosInventario
            });
        }
    }

    // ViewModels para reportes
    public class VentasReporteViewModel
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string TipoReporte { get; set; }
        public List<Ventum> Ventas { get; set; } = new();
        public int TotalVentas { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio { get; set; }
        public List<ResumenVendedor> ResumenPorVendedor { get; set; } = new();
        public List<ResumenDiario> ResumenPorDia { get; set; } = new();
        public List<TopProducto> TopProductos { get; set; } = new();
    }

    public class CajaReporteViewModel
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<MovimientoCaja> Movimientos { get; set; } = new();
        public int TotalMovimientos { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal SaldoFinal { get; set; }
        public List<ResumenTipoMovimiento> ResumenPorTipo { get; set; } = new();
        public List<ResumenCajaDiario> ResumenPorDia { get; set; } = new();
    }

    public class InventarioReporteViewModel
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string TipoReporte { get; set; }
        public List<MovimientoInventario> Movimientos { get; set; } = new();
        public int TotalMovimientos { get; set; }
        public List<ResumenInventarioTipo> ResumenPorTipo { get; set; } = new();
        public List<ResumenProductoInventario> ResumenPorProducto { get; set; } = new();
        public List<ResumenInventarioDiario> ResumenPorDia { get; set; } = new();
        public List<TopProductoMovimiento> TopProductosMovimiento { get; set; } = new();
    }

    // Clases de resumen
    public class ResumenVendedor
    {
        public string Vendedor { get; set; }
        public int CantidadVentas { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio { get; set; }
    }

    public class ResumenDiario
    {
        public DateTime Fecha { get; set; }
        public int CantidadVentas { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio { get; set; }
    }

    public class TopProducto
    {
        public string Producto { get; set; }
        public int CantidadTotal { get; set; }
        public decimal MontoTotal { get; set; }
    }

    public class ResumenTipoMovimiento
    {
        public string Tipo { get; set; }
        public int Cantidad { get; set; }
        public decimal MontoTotal { get; set; }
    }

    public class ResumenCajaDiario
    {
        public DateTime Fecha { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal Neto { get; set; }
    }

    public class ResumenInventarioTipo
    {
        public string Tipo { get; set; }
        public int Cantidad { get; set; }
        public int ProductosAfectados { get; set; }
    }

    public class ResumenProductoInventario
    {
        public string Producto { get; set; }
        public int Ingresos { get; set; }
        public int Egresos { get; set; }
        public int Neto { get; set; }
    }

    public class ResumenInventarioDiario
    {
        public DateTime Fecha { get; set; }
        public int Ingresos { get; set; }
        public int Egresos { get; set; }
        public int Neto { get; set; }
    }

    public class TopProductoMovimiento
    {
        public string Producto { get; set; }
        public int TotalMovimientos { get; set; }
        public int CantidadTotal { get; set; }
    }
}
