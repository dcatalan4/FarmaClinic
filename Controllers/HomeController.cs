using System.Diagnostics;
using System.Security.Claims;
using ControlInventario.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ControlFarmaclinicContext _context;

        public HomeController(ILogger<HomeController> logger, ControlFarmaclinicContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Si es Lector, redirigir directamente al inventario
            if (User.IsInRole("Lector"))
            {
                return RedirectToAction("Index", "InventarioLector");
            }

            var hoy = DateTime.Today;
            var primerDiaMes = new DateTime(hoy.Year, hoy.Month, 1);
            var fecha30Dias = hoy.AddDays(-30);
            var primerDiaSemana = hoy.AddDays(-(int)hoy.DayOfWeek);
            var primerDiaAno = new DateTime(hoy.Year, 1, 1);
            var primerDiaMesAnterior = primerDiaMes.AddMonths(-1);
            var ultimoDiaMesAnterior = primerDiaMes.AddDays(-1);

            // Obtener información del usuario actual
            var userIdClaim = User.FindFirst("IdUsuario")?.Value;
            var userNameClaim = User.FindFirst("Usuario")?.Value;
            var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (User.IsInRole("Admin"))
            {
                // Dashboard para Administrador
                var adminModel = new DashboardAdminViewModel();

                // Productos
                adminModel.TotalProductos = await _context.Productos.CountAsync();
                adminModel.ProductosActivos = await _context.Productos.CountAsync(p => p.Activo == true);
                adminModel.ProductosBajoStock = await _context.Productos.CountAsync(p => p.Activo == true && p.StockActual < 10);
                adminModel.ProductosSinStock = await _context.Productos.CountAsync(p => p.Activo == true && p.StockActual == 0);
                adminModel.ValorTotalInventario = await _context.Productos
                    .Where(p => p.Activo == true)
                    .SumAsync(p => p.StockActual * p.PrecioIngreso);

                // Ventas hoy
                var hoyInicio = hoy.Date;
                var hoyFin = hoy.Date.AddDays(1).AddTicks(-1);
                
                var ventasHoyQuery = _context.Venta
                    .Where(v => v.Fecha.HasValue && 
                               v.Fecha.Value >= hoyInicio && 
                               v.Fecha.Value <= hoyFin && 
                               v.Anulada == false);
                
                adminModel.VentasHoy = await ventasHoyQuery.CountAsync();
                adminModel.VentasHoyMonto = await ventasHoyQuery.SumAsync(v => v.Total);

                // Ventas semana
                adminModel.VentasSemana = await _context.Venta.CountAsync(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaSemana && v.Anulada == false);
                adminModel.VentasSemanaMonto = await _context.Venta
                    .Where(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaSemana && v.Anulada == false)
                    .SumAsync(v => v.Total);

                // Ventas mes
                adminModel.VentasMes = await _context.Venta.CountAsync(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaMes && v.Anulada == false);
                adminModel.VentasMesMonto = await _context.Venta
                    .Where(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaMes && v.Anulada == false)
                    .SumAsync(v => v.Total);

                // Cálculo de ticket promedio
                if (adminModel.VentasMes > 0)
                {
                    adminModel.TicketPromedio = adminModel.VentasMesMonto / adminModel.VentasMes;
                }

                // Crecimiento de ventas mes vs mes anterior
                var ventasMesAnterior = await _context.Venta.CountAsync(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaMesAnterior && v.Fecha.Value <= ultimoDiaMesAnterior && v.Anulada == false);
                if (ventasMesAnterior > 0)
                {
                    adminModel.CrecimientoVentasMes = ((decimal)(adminModel.VentasMes - ventasMesAnterior) / ventasMesAnterior) * 100;
                }

                // Caja
                adminModel.SaldoCajaPrincipal = await _context.Cajas
                    .Where(c => c.Activa == true)
                    .SumAsync(c => c.SaldoActual);
                adminModel.CajasActivas = await _context.Cajas.CountAsync(c => c.Activa == true);

                // Usuarios
                adminModel.TotalUsuarios = await _context.Usuarios.CountAsync();
                adminModel.UsuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo == true);

                // Productos más vendidos (últimos 30 días) - ahora 10
                adminModel.ProductosMasVendidos = await _context.DetalleVenta
                    .Include(dv => dv.IdProductoNavigation)
                    .Include(dv => dv.IdVentaNavigation)
                    .Where(dv => dv.IdVentaNavigation.Fecha.HasValue &&
                                 dv.IdVentaNavigation.Fecha.Value >= fecha30Dias &&
                                 dv.IdVentaNavigation.Anulada == false)
                    .GroupBy(dv => dv.IdProductoNavigation.Nombre)
                    .Select(g => new ProductoTop
                    {
                        Nombre = g.Key,
                        CantidadVendida = g.Sum(dv => dv.Cantidad),
                        TotalVendido = g.Sum(dv => dv.Subtotal)
                    })
                    .OrderByDescending(p => p.CantidadVendida)
                    .Take(10)
                    .ToListAsync();

                // Ventas recientes
                adminModel.VentasRecientes = await _context.Venta
                    .Include(v => v.IdUsuarioNavigation)
                    .Where(v => v.Fecha.HasValue && v.Anulada == false)
                    .OrderByDescending(v => v.Fecha.Value)
                    .Take(5)
                    .Select(v => new VentaReciente
                    {
                        NumeroVenta = v.NumeroVenta.ToString(),
                        Fecha = v.Fecha.Value,
                        Total = v.Total,
                        UsuarioNombre = v.IdUsuarioNavigation.Nombre
                    })
                    .ToListAsync();

                return View("DashboardAdmin", adminModel);
            }
            else
            {
                // Dashboard para Vendedor
                var vendedorModel = new DashboardVendedorViewModel();
                int currentUserId = int.Parse(userIdClaim);

                // Ventas personales hoy
                vendedorModel.MisVentasHoy = await _context.Venta
                    .CountAsync(v => v.Fecha.HasValue && v.Fecha.Value.Date == hoy && v.Anulada == false && v.IdUsuario == currentUserId);
                vendedorModel.MisVentasHoyMonto = await _context.Venta
                    .Where(v => v.Fecha.HasValue && v.Fecha.Value.Date == hoy && v.Anulada == false && v.IdUsuario == currentUserId)
                    .SumAsync(v => v.Total);

                // Ventas personales semana
                vendedorModel.MisVentasSemana = await _context.Venta
                    .CountAsync(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaSemana && v.Anulada == false && v.IdUsuario == currentUserId);
                vendedorModel.MisVentasSemanaMonto = await _context.Venta
                    .Where(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaSemana && v.Anulada == false && v.IdUsuario == currentUserId)
                    .SumAsync(v => v.Total);

                // Ventas personales mes
                vendedorModel.MisVentasMes = await _context.Venta
                    .CountAsync(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaMes && v.Anulada == false && v.IdUsuario == currentUserId);
                vendedorModel.MisVentasMesMonto = await _context.Venta
                    .Where(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaMes && v.Anulada == false && v.IdUsuario == currentUserId)
                    .SumAsync(v => v.Total);

                // Cálculo de ticket promedio personal
                if (vendedorModel.MisVentasMes > 0)
                {
                    vendedorModel.MiTicketPromedio = vendedorModel.MisVentasMesMonto / vendedorModel.MisVentasMes;
                }

                // Crecimiento de ventas personales mes vs mes anterior
                var ventasMesAnteriorPersonal = await _context.Venta.CountAsync(v => v.Fecha.HasValue && v.Fecha.Value >= primerDiaMesAnterior && v.Fecha.Value <= ultimoDiaMesAnterior && v.Anulada == false && v.IdUsuario == currentUserId);
                if (ventasMesAnteriorPersonal > 0)
                {
                    vendedorModel.MiCrecimientoVentasMes = ((decimal)(vendedorModel.MisVentasMes - ventasMesAnteriorPersonal) / ventasMesAnteriorPersonal) * 100;
                }

                // Productos
                vendedorModel.ProductosActivos = await _context.Productos.CountAsync(p => p.Activo == true);
                vendedorModel.ProductosBajoStock = await _context.Productos.CountAsync(p => p.Activo == true && p.StockActual < 10);
                vendedorModel.MisProductosSinStock = await _context.Productos.CountAsync(p => p.Activo == true && p.StockActual == 0);

                // Caja
                vendedorModel.SaldoCajaPrincipal = await _context.Cajas
                    .Where(c => c.Activa == true)
                    .SumAsync(c => c.SaldoActual);

                // Mis productos más vendidos (últimos 30 días) - ahora 10
                vendedorModel.MisProductosMasVendidos = await _context.DetalleVenta
                    .Include(dv => dv.IdProductoNavigation)
                    .Include(dv => dv.IdVentaNavigation)
                    .Where(dv => dv.IdVentaNavigation.Fecha.HasValue &&
                                 dv.IdVentaNavigation.Fecha.Value >= fecha30Dias &&
                                 dv.IdVentaNavigation.Anulada == false &&
                                 dv.IdVentaNavigation.IdUsuario == currentUserId)
                    .GroupBy(dv => dv.IdProductoNavigation.Nombre)
                    .Select(g => new ProductoTop
                    {
                        Nombre = g.Key,
                        CantidadVendida = g.Sum(dv => dv.Cantidad),
                        TotalVendido = g.Sum(dv => dv.Subtotal)
                    })
                    .OrderByDescending(p => p.CantidadVendida)
                    .Take(10)
                    .ToListAsync();

                // Mis ventas recientes
                vendedorModel.MisVentasRecientes = await _context.Venta
                    .Include(v => v.IdUsuarioNavigation)
                    .Where(v => v.Fecha.HasValue && v.Anulada == false && v.IdUsuario == currentUserId)
                    .OrderByDescending(v => v.Fecha.Value)
                    .Take(5)
                    .Select(v => new VentaReciente
                    {
                        NumeroVenta = v.NumeroVenta.ToString(),
                        Fecha = v.Fecha.Value,
                        Total = v.Total,
                        UsuarioNombre = v.IdUsuarioNavigation.Nombre
                    })
                    .ToListAsync();

                // Información del usuario
                vendedorModel.NombreUsuario = User.Identity.Name;
                vendedorModel.RolUsuario = userRoleClaim;

                return View("DashboardVendedor", vendedorModel);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
