using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Models;
using ControlInventario.Helpers;
using System.Security.Claims;

namespace ControlInventario.Controllers
{
    [Authorize]
    public class CajaController : Controller
    {
        private readonly ControlFarmaclinicContext _context;

        public CajaController(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        // GET: Caja
        public async Task<IActionResult> Index()
        {
            // Obtener la caja principal (ID = 1) o la primera activa
            var cajaPrincipal = await _context.Cajas
                .Where(c => c.Activa == true)
                .FirstOrDefaultAsync();

            if (cajaPrincipal == null)
            {
                // Si no hay cajas activas, tomar la primera o crear una por defecto
                cajaPrincipal = await _context.Cajas.FirstOrDefaultAsync();
                if (cajaPrincipal == null)
                {
                    // Crear caja principal por defecto
                    cajaPrincipal = new Caja
                    {
                        Nombre = "Caja Principal",
                        SaldoActual = 0,
                        Activa = true,
                        FechaCreacion = DateTimeHelper.GetClientDateTime()
                    };
                    _context.Cajas.Add(cajaPrincipal);
                    await _context.SaveChangesAsync();
                }
            }

            // Obtener movimientos recientes
            var movimientos = await _context.MovimientoCajas
                .Include(m => m.IdUsuarioNavigation)
                .Where(m => m.IdCaja == cajaPrincipal.IdCaja)
                .OrderByDescending(m => m.Fecha)
                .Take(50)
                .ToListAsync();

            ViewBag.Caja = cajaPrincipal;
            return View(movimientos);
        }

        // GET: Caja/Movimientos
        public async Task<IActionResult> Movimientos(DateTime? fechaInicio, DateTime? fechaFin)
        {
            var cajaPrincipal = await _context.Cajas
                .Where(c => c.Activa == true)
                .FirstOrDefaultAsync();

            if (cajaPrincipal == null)
            {
                TempData["Error"] = "No hay una caja principal activa.";
                return RedirectToAction("Index");
            }

            var query = _context.MovimientoCajas
                .Include(m => m.IdUsuarioNavigation)
                .Where(m => m.IdCaja == cajaPrincipal.IdCaja);

            // Aplicar filtros de fecha si se proporcionan
            if (fechaInicio.HasValue)
            {
                // Convertir a UTC para comparar con fechas guardadas en UTC
                var inicio = DateTime.SpecifyKind(fechaInicio.Value.Date, DateTimeKind.Utc);
                query = query.Where(m => m.Fecha.HasValue && m.Fecha.Value >= inicio);
            }

            if (fechaFin.HasValue)
            {
                // Fin del día en UTC (23:59:59)
                var fin = DateTime.SpecifyKind(fechaFin.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(m => m.Fecha.HasValue && m.Fecha.Value <= fin);
            }

            var movimientos = await query
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            ViewBag.Caja = cajaPrincipal;
            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;

            return View(movimientos);
        }

        // GET: Caja/Agregar
        public IActionResult Agregar()
        {
            ViewBag.Motivos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Venta del día", Text = "Venta del día" },
                new SelectListItem { Value = "Depósito inicial", Text = "Depósito inicial" },
                new SelectListItem { Value = "Transferencia bancaria", Text = "Transferencia bancaria" },
                new SelectListItem { Value = "Ajuste de saldo", Text = "Ajuste de saldo" },
                new SelectListItem { Value = "Otros ingresos", Text = "Otros ingresos" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };

            var cajaPrincipal = _context.Cajas
                .Where(c => c.Activa == true)
                .FirstOrDefault() ?? _context.Cajas.FirstOrDefault();

            ViewBag.Caja = cajaPrincipal;
            return View();
        }

        // POST: Caja/Agregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(decimal Monto, string Concepto, string motivoSeleccionado, string otroMotivo, string clientDateTime = null)
        {
            try
            {
                Console.WriteLine("Iniciando operación de agregar dinero a caja...");
                Console.WriteLine($"Monto recibido: {Monto}");
                Console.WriteLine($"Motivo seleccionado: '{motivoSeleccionado}'");
                Console.WriteLine($"Otro motivo: '{otroMotivo}'");
                Console.WriteLine($"Concepto: '{Concepto}'");
                Console.WriteLine($"Fecha del cliente: {clientDateTime}");
                Console.WriteLine($"Monto es null: {Monto == null}");
                Console.WriteLine($"motivoSeleccionado es null: {motivoSeleccionado == null}");
                Console.WriteLine($"otroMotivo es null: {otroMotivo}");
                Console.WriteLine($"Concepto es null: {Concepto == null}");

                // Parsear fecha del cliente
                DateTime? clientDate = null;
                if (!string.IsNullOrEmpty(clientDateTime))
                {
                    string[] formats = { "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
                    if (DateTime.TryParseExact(clientDateTime, formats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        clientDate = parsedDate;
                        Console.WriteLine($"Fecha del cliente parseada: {parsedDate}");
                    }
                }
                
                var cajaPrincipal = await _context.Cajas
                    .Where(c => c.Activa == true)
                    .FirstOrDefaultAsync() ?? await _context.Cajas.FirstOrDefaultAsync();

                if (cajaPrincipal == null)
                {
                    Console.WriteLine("Error: No hay una caja configurada");
                    return Json(new { success = false, message = "No hay una caja configurada" });
                }

                Console.WriteLine($"Caja encontrada: {cajaPrincipal.Nombre}, Saldo actual: Q{cajaPrincipal.SaldoActual}");

                // Validar monto
                if (Monto <= 0)
                {
                    Console.WriteLine("Error: Monto inválido");
                    return Json(new { success = false, message = "El monto debe ser mayor a cero" });
                }

                // Validar motivo
                if (string.IsNullOrEmpty(motivoSeleccionado))
                {
                    Console.WriteLine("Error: Motivo no seleccionado");
                    return Json(new { success = false, message = "Debe seleccionar un motivo para el movimiento" });
                }

                // Validar motivo "Otro"
                if (motivoSeleccionado == "Otro" && string.IsNullOrEmpty(otroMotivo))
                {
                    Console.WriteLine("Error: Motivo 'Otro' sin especificar");
                    return Json(new { success = false, message = "Debe especificar el motivo cuando selecciona 'Otro'" });
                }

                // Determinar el concepto
                string conceptoFinal = motivoSeleccionado;
                if (motivoSeleccionado == "Otro" && !string.IsNullOrEmpty(otroMotivo))
                {
                    conceptoFinal = otroMotivo;
                }
                else if (!string.IsNullOrEmpty(Concepto))
                {
                    conceptoFinal += $" - {Concepto}";
                }

                Console.WriteLine($"Concepto final: {conceptoFinal}");
                Console.WriteLine($"Monto a agregar: Q{Monto}");

                // Crear movimiento
                var nuevoMovimiento = new MovimientoCaja
                {
                    IdCaja = cajaPrincipal.IdCaja,
                    TipoMovimiento = "I", // Ingreso (Entrada)
                    Monto = Monto,
                    Fecha = DateTimeHelper.GetClientDateTime(clientDate),
                    Concepto = conceptoFinal,
                    IdUsuario = ObtenerUsuarioActual(),
                    IdReferencia = null
                };

                // Actualizar saldo de la caja
                decimal saldoAnterior = cajaPrincipal.SaldoActual;
                cajaPrincipal.SaldoActual += Monto;

                Console.WriteLine($"Saldo anterior: Q{saldoAnterior}");
                Console.WriteLine($"Saldo nuevo: Q{cajaPrincipal.SaldoActual}");

                _context.MovimientoCajas.Add(nuevoMovimiento);
                await _context.SaveChangesAsync();

                Console.WriteLine("Movimiento guardado exitosamente");

                var resumen = new
                {
                    tipoOperacion = "Ingreso",
                    monto = Monto,
                    concepto = conceptoFinal,
                    saldoAnterior = saldoAnterior,
                    saldoNuevo = cajaPrincipal.SaldoActual,
                    fecha = DateTimeHelper.FormatClientDateTime(DateTimeHelper.GetClientDateTime(clientDate), "dd/MM/yyyy HH:mm:ss"),
                    caja = cajaPrincipal.Nombre
                };

                return Json(new { 
                    success = true, 
                    message = $"Se agregaron Q{Monto:F2} a la caja correctamente",
                    resumen = resumen
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Agregar: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error al procesar el ingreso: " + ex.Message });
            }
        }

        // GET: Caja/Retirar
        public IActionResult Retirar()
        {
            ViewBag.Motivos = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pago de proveedores", Text = "Pago de proveedores" },
                new SelectListItem { Value = "Gastos operativos", Text = "Gastos operativos" },
                new SelectListItem { Value = "Retiro de socio", Text = "Retiro de socio" },
                new SelectListItem { Value = "Transferencia bancaria", Text = "Transferencia bancaria" },
                new SelectListItem { Value = "Ajuste de saldo", Text = "Ajuste de saldo" },
                new SelectListItem { Value = "Otros egresos", Text = "Otros egresos" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };

            var cajaPrincipal = _context.Cajas
                .Where(c => c.Activa == true)
                .FirstOrDefault() ?? _context.Cajas.FirstOrDefault();

            ViewBag.Caja = cajaPrincipal;
            return View();
        }

        // POST: Caja/Retirar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retirar(decimal Monto, string Concepto, string motivoSeleccionado, string otroMotivo, string clientDateTime = null)
        {
            try
            {
                Console.WriteLine("Iniciando operación de retirar dinero de caja...");
                Console.WriteLine($"Monto recibido: {Monto}");
                Console.WriteLine($"Motivo seleccionado: '{motivoSeleccionado}'");
                Console.WriteLine($"Otro motivo: '{otroMotivo}'");
                Console.WriteLine($"Concepto: '{Concepto}'");
                Console.WriteLine($"Fecha del cliente: {clientDateTime}");

                // Parsear fecha del cliente
                DateTime? clientDate = null;
                if (!string.IsNullOrEmpty(clientDateTime))
                {
                    string[] formats = { "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss" };
                    if (DateTime.TryParseExact(clientDateTime, formats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                    {
                        clientDate = parsedDate;
                        Console.WriteLine($"Fecha del cliente parseada: {parsedDate}");
                    }
                }
                
                var cajaPrincipal = await _context.Cajas
                    .Where(c => c.Activa == true)
                    .FirstOrDefaultAsync() ?? await _context.Cajas.FirstOrDefaultAsync();

                if (cajaPrincipal == null)
                {
                    Console.WriteLine("Error: No hay una caja configurada");
                    return Json(new { success = false, message = "No hay una caja configurada" });
                }

                Console.WriteLine($"Caja encontrada: {cajaPrincipal.Nombre}, Saldo actual: Q{cajaPrincipal.SaldoActual}");

                // Validar monto
                if (Monto <= 0)
                {
                    Console.WriteLine("Error: Monto inválido");
                    return Json(new { success = false, message = "El monto debe ser mayor a cero" });
                }

                // Validar motivo
                if (string.IsNullOrEmpty(motivoSeleccionado))
                {
                    Console.WriteLine("Error: Motivo no seleccionado");
                    return Json(new { success = false, message = "Debe seleccionar un motivo para el movimiento" });
                }

                // Validar motivo "Otro"
                if (motivoSeleccionado == "Otro" && string.IsNullOrEmpty(otroMotivo))
                {
                    Console.WriteLine("Error: Motivo 'Otro' sin especificar");
                    return Json(new { success = false, message = "Debe especificar el motivo cuando selecciona 'Otro'" });
                }

                // Validar que haya suficiente saldo
                if (Monto > cajaPrincipal.SaldoActual)
                {
                    Console.WriteLine($"Error: Saldo insuficiente. Intenta retirar Q{Monto}, pero solo hay Q{cajaPrincipal.SaldoActual}");
                    return Json(new { success = false, message = $"Saldo insuficiente. Saldo actual: Q{cajaPrincipal.SaldoActual:F2}" });
                }

                // Determinar el concepto
                string conceptoFinal = motivoSeleccionado;
                if (motivoSeleccionado == "Otro" && !string.IsNullOrEmpty(otroMotivo))
                {
                    conceptoFinal = otroMotivo;
                }
                else if (!string.IsNullOrEmpty(Concepto))
                {
                    conceptoFinal += $" - {Concepto}";
                }

                Console.WriteLine($"Concepto final: {conceptoFinal}");
                Console.WriteLine($"Monto a retirar: Q{Monto}");

                // Crear movimiento
                var nuevoMovimiento = new MovimientoCaja
                {
                    IdCaja = cajaPrincipal.IdCaja,
                    TipoMovimiento = "E", // Egreso (Salida)
                    Monto = Monto,
                    Fecha = DateTimeHelper.GetClientDateTime(clientDate),
                    Concepto = conceptoFinal,
                    IdUsuario = ObtenerUsuarioActual(),
                    IdReferencia = null
                };

                // Actualizar saldo de la caja
                decimal saldoAnterior = cajaPrincipal.SaldoActual;
                cajaPrincipal.SaldoActual -= Monto;

                Console.WriteLine($"Saldo anterior: Q{saldoAnterior}");
                Console.WriteLine($"Saldo nuevo: Q{cajaPrincipal.SaldoActual}");

                _context.MovimientoCajas.Add(nuevoMovimiento);
                await _context.SaveChangesAsync();

                Console.WriteLine("Movimiento guardado exitosamente");

                var resumen = new
                {
                    tipoOperacion = "Egreso",
                    monto = Monto,
                    concepto = conceptoFinal,
                    saldoAnterior = saldoAnterior,
                    saldoNuevo = cajaPrincipal.SaldoActual,
                    fecha = DateTimeHelper.FormatClientDateTime(DateTimeHelper.GetClientDateTime(clientDate), "dd/MM/yyyy HH:mm:ss"),
                    caja = cajaPrincipal.Nombre
                };

                return Json(new { 
                    success = true, 
                    message = $"Se retiraron Q{Monto:F2} de la caja correctamente",
                    resumen = resumen
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Retirar: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error al procesar el retiro: " + ex.Message });
            }
        }

        // GET: Caja/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movimientoCaja = await _context.MovimientoCajas
                .Include(m => m.IdCajaNavigation)
                .Include(m => m.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.IdMovimientoCaja == id);

            if (movimientoCaja == null)
            {
                return NotFound();
            }

            return View(movimientoCaja);
        }

        private int ObtenerUsuarioActual()
        {
            try
            {
                // Obtener el ID del usuario autenticado actual
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
                
                // Si no se encuentra el claim, intentar con el ID del usuario
                var usuarioClaim = User.FindFirst("Usuario");
                if (usuarioClaim != null && int.TryParse(usuarioClaim.Value, out int usuarioId))
                {
                    return usuarioId;
                }
                
                // Como último recurso, buscar por nombre de usuario
                var userName = User.Identity?.Name;
                if (!string.IsNullOrEmpty(userName))
                {
                    var usuario = _context.Usuarios.FirstOrDefault(u => u.Nombre == userName);
                    if (usuario != null)
                    {
                        return usuario.IdUsuario;
                    }
                }
                
                // Si no se encuentra ningún usuario, retornar 1 como valor por defecto
                // Esto debería ser temporal hasta que la autenticación esté completamente configurada
                return 1;
            }
            catch
            {
                // En caso de error, retornar 1 como valor por defecto
                return 1;
            }
        }
    }
}
