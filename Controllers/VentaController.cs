using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Models;

namespace ControlInventario.Controllers
{
    [Authorize]
    public class VentaController : Controller
    {
        private readonly ControlFarmaclinicContext _context;

        public VentaController(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        // GET: Venta
        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Por defecto mostrar ventas del día de hoy
            var hoy = DateTime.Today;
            var inicio = fechaInicio ?? hoy;
            var fin = fechaFin ?? hoy.AddDays(1).AddTicks(-1); // Fin del día

            // Obtener el ID del usuario actual
            var userIdClaim = User.FindFirst("IdUsuario")?.Value;
            int currentUserId = 0;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                int.TryParse(userIdClaim, out currentUserId);
            }

            var query = _context.Venta
                .Include(v => v.IdUsuarioNavigation)
                .Where(v => v.Anulada != true && 
                           v.Fecha.HasValue && 
                           v.Fecha.Value >= inicio && 
                           v.Fecha.Value <= fin);

            // Si no es admin, mostrar solo sus ventas
            if (!User.IsInRole("Admin") && currentUserId > 0)
            {
                query = query.Where(v => v.IdUsuario == currentUserId);
            }

            var ventas = await query.OrderByDescending(v => v.Fecha).ToListAsync();

            // Pasar las fechas a la vista para mantener los filtros
            ViewBag.FechaInicio = inicio.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fin.ToString("yyyy-MM-dd");
            ViewBag.EsAdmin = User.IsInRole("Admin");

            return View(ventas);
        }

        // GET: Venta/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ventum = await _context.Venta
                .Include(v => v.IdUsuarioNavigation)
                .Include(v => v.DetalleVenta)
                .ThenInclude(dv => dv.IdProductoNavigation)
                .FirstOrDefaultAsync(m => m.IdVenta == id);
            
            if (ventum == null)
            {
                return NotFound();
            }

            return View(ventum);
        }

        // GET: Venta/Create
        public IActionResult Create()
        {
            // El usuario se obtiene de la sesión, no se necesita selector
            return View();
        }

        // POST: Venta/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("IdVenta,Fecha,Subtotal,Total,Anulada")] Ventum ventum,
            List<int> IdProducto, List<int> Cantidad, List<decimal> PrecioUnitario)
        {
            // Obtener el ID del usuario logueado
            var userIdClaim = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }
            
            ventum.IdUsuario = int.Parse(userIdClaim);
            
            // Debug: Imprimir valores recibidos
            Console.WriteLine($"=== DEBUG CREATE VENTA ===");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            Console.WriteLine($"IdUsuario (desde sesión): {ventum.IdUsuario}");
            Console.WriteLine($"IdProducto count: {IdProducto?.Count}");
            Console.WriteLine($"Cantidad count: {Cantidad?.Count}");
            Console.WriteLine($"PrecioUnitario count: {PrecioUnitario?.Count}");
            
            if (IdProducto != null)
            {
                for (int i = 0; i < IdProducto.Count; i++)
                {
                    Console.WriteLine($"Producto[{i}]: {IdProducto[i]}, Cantidad[{i}]: {Cantidad?[i]}, Precio[{i}]: {PrecioUnitario?[i]}");
                }
            }
            
            // Validar que se hayan enviado productos
            if (IdProducto == null || IdProducto.All(id => id == 0))
            {
                Console.WriteLine("ERROR: No se enviaron productos válidos");
                ModelState.AddModelError("", "Debe agregar al menos un producto a la venta");
                ViewData["IdUsuario"] = new SelectList(_context.Usuarios.Where(u => u.Activo == true), "IdUsuario", "Nombre", ventum.IdUsuario);
                return View(ventum);
            }

            if (ModelState.IsValid)
            {
                Console.WriteLine("ModelState es válido, procesando venta...");
                // Generar número de venta único
                var numeroVenta = int.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
                ventum.NumeroVenta = numeroVenta;

                ventum.Fecha = DateTime.Now;
                ventum.Anulada = false;

                // Calcular subtotal y total
                decimal subtotal = 0;
                var detallesVenta = new List<DetalleVentum>();

                for (int i = 0; i < IdProducto.Count; i++)
                {
                    if (IdProducto[i] > 0 && Cantidad[i] > 0)
                    {
                        var producto = await _context.Productos.FindAsync(IdProducto[i]);
                        if (producto != null && producto.StockActual >= Cantidad[i])
                        {
                            var precioUnit = PrecioUnitario[i];
                            var subtotalDetalle = Cantidad[i] * precioUnit;
                            subtotal += subtotalDetalle;

                            var detalle = new DetalleVentum
                            {
                                IdProducto = IdProducto[i],
                                Cantidad = Cantidad[i],
                                PrecioUnitario = precioUnit,
                                PrecioIngreso = producto.PrecioIngreso,
                                Subtotal = subtotalDetalle
                            };
                            detallesVenta.Add(detalle);

                            // Actualizar stock
                            producto.StockActual -= Cantidad[i];
                            
                            // Crear movimiento de inventario por la venta
                            var movimientoInventario = new MovimientoInventario
                            {
                                IdProducto = IdProducto[i],
                                TipoMovimiento = "S", // S para Salida (venta de producto)
                                Cantidad = Cantidad[i],
                                Fecha = DateTime.Now,
                                Referencia = $"Venta #{ventum.NumeroVenta}",
                                IdUsuario = ventum.IdUsuario
                            };
                            _context.Add(movimientoInventario);
                        }
                        else
                        {
                            Console.WriteLine($"ERROR: Stock insuficiente para producto {producto?.Nombre}");
                            ModelState.AddModelError("", $"No hay stock suficiente para el producto {producto?.Nombre}");
                            ViewData["IdUsuario"] = new SelectList(_context.Usuarios.Where(u => u.Activo == true), "IdUsuario", "Nombre", ventum.IdUsuario);
                            return View(ventum);
                        }
                    }
                }

                ventum.Subtotal = subtotal;
                ventum.Total = subtotal; // Si deseas IVA, se calcula aquí
                ventum.Activa = true; // Establecer venta como activa
                ventum.Anulada = false; // Establecer como no anulada

                _context.Add(ventum);
                await _context.SaveChangesAsync();

                // Agregar detalles de venta
                foreach (var detalle in detallesVenta)
                {
                    detalle.IdVenta = ventum.IdVenta;
                    _context.Add(detalle);
                }
                await _context.SaveChangesAsync();

                // Actualizar caja principal
                var cajaPrincipal = await _context.Cajas
                    .Where(c => c.Activa == true)
                    .FirstOrDefaultAsync();

                if (cajaPrincipal != null)
                {
                    // Actualizar saldo de la caja
                    cajaPrincipal.SaldoActual += ventum.Total;
                    
                    // Crear movimiento de caja
                    var movimientoCaja = new MovimientoCaja
                    {
                        IdCaja = cajaPrincipal.IdCaja,
                        TipoMovimiento = "I", // I = Ingreso
                        Monto = ventum.Total,
                        Fecha = DateTime.Now,
                        Concepto = $"Venta #{ventum.NumeroVenta}",
                        IdReferencia = ventum.IdVenta,
                        IdUsuario = ventum.IdUsuario
                    };
                    
                    _context.Add(movimientoCaja);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"Caja principal actualizada: +Q {ventum.Total:N2}");
                }
                else
                {
                    Console.WriteLine("ADVERTENCIA: No se encontró caja principal activa");
                }

                Console.WriteLine("Venta creada con éxito");
                
                // Guardar mensaje de éxito en TempData
                TempData["SuccessMessage"] = $"¡Venta #{ventum.NumeroVenta} registrada exitosamente por Q {ventum.Total:N2}!";
                
                return RedirectToAction(nameof(Create));
            }
            else
            {
                Console.WriteLine("ModelState NO es válido:");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
            }

            ViewData["IdUsuario"] = new SelectList(_context.Usuarios.Where(u => u.Activo == true), "IdUsuario", "Nombre", ventum.IdUsuario);
            Console.WriteLine("Retornando vista Create con errores");
            return View(ventum);
        }


        // GET: Venta/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ventum = await _context.Venta.FindAsync(id);
            if (ventum == null)
            {
                return NotFound();
            }
            ViewData["IdUsuario"] = new SelectList(_context.Usuarios, "IdUsuario", "Nombre", ventum.IdUsuario);
            return View(ventum);
        }

        // POST: Venta/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdVenta,NumeroVenta,Fecha,IdUsuario,Subtotal,Total,Anulada")] Ventum ventum)
        {
            if (id != ventum.IdVenta)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ventum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VentumExists(ventum.IdVenta))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdUsuario"] = new SelectList(_context.Usuarios, "IdUsuario", "Nombre", ventum.IdUsuario);
            return View(ventum);
        }

        // GET: Venta/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ventum = await _context.Venta
                .Include(v => v.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.IdVenta == id);
            if (ventum == null)
            {
                return NotFound();
            }

            return View(ventum);
        }

        // POST: Venta/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ventum = await _context.Venta.FindAsync(id);
            if (ventum != null)
            {
                // En lugar de eliminar, marcar como anulada e inactiva
                ventum.Anulada = true;
                ventum.Activa = false;
                
                // Devolver productos al stock
                var detallesVenta = await _context.DetalleVenta
                    .Where(dv => dv.IdVenta == id)
                    .Include(dv => dv.IdProductoNavigation)
                    .ToListAsync();

                foreach (var detalle in detallesVenta)
                {
                    detalle.IdProductoNavigation.StockActual += detalle.Cantidad;
                    
                    // Crear movimiento de inventario por la devolución de stock
                    var movimientoInventario = new MovimientoInventario
                    {
                        IdProducto = detalle.IdProducto,
                        TipoMovimiento = "E", // E para Entrada (devolución por anulación)
                        Cantidad = detalle.Cantidad,
                        Fecha = DateTime.Now,
                        Referencia = $"Anulación Venta #{ventum.NumeroVenta}",
                        IdUsuario = ventum.IdUsuario
                    };
                    _context.Add(movimientoInventario);
                }

                // Devolver dinero a la caja principal
                var cajaPrincipal = await _context.Cajas
                    .Where(c => c.Activa == true)
                    .FirstOrDefaultAsync();

                if (cajaPrincipal != null)
                {
                    // Actualizar saldo de la caja (restar el monto de la venta anulada)
                    cajaPrincipal.SaldoActual -= ventum.Total;
                    
                    // Crear movimiento de caja de salida
                    var movimientoCaja = new MovimientoCaja
                    {
                        IdCaja = cajaPrincipal.IdCaja,
                        TipoMovimiento = "E", // E = Egreso
                        Monto = ventum.Total,
                        Fecha = DateTime.Now,
                        Concepto = $"Anulación Venta #{ventum.NumeroVenta}",
                        IdReferencia = ventum.IdVenta,
                        IdUsuario = ventum.IdUsuario
                    };
                    
                    _context.Add(movimientoCaja);
                    Console.WriteLine($"Caja principal actualizada: -Q {ventum.Total:N2} (anulación venta)");
                }
                else
                {
                    Console.WriteLine("ADVERTENCIA: No se encontró caja principal activa para devolución");
                }

                _context.Update(ventum);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Venta/BuscarInventarioCompleto
        [HttpGet]
        public async Task<JsonResult> BuscarInventarioCompleto(string termino = "")
        {
            try
            {
                var query = _context.Productos.Where(p => p.Activo == true && p.StockActual > 0);

                if (!string.IsNullOrWhiteSpace(termino))
                {
                    query = query.Where(p => 
                        p.Codigo.Contains(termino) ||
                        p.Nombre.Contains(termino) ||
                        (p.Descripcion != null && p.Descripcion.Contains(termino))
                    );
                }

                var productos = await query
                    .OrderBy(p => p.Nombre)
                    .Select(p => new
                    {
                        idProducto = p.IdProducto,
                        codigo = p.Codigo,
                        nombre = p.Nombre,
                        descripcion = p.Descripcion,
                        precioVenta = p.PrecioVenta,
                        stockActual = p.StockActual
                    })
                    .Take(100) // Limitar a 100 resultados para mejor rendimiento
                    .ToListAsync();

                return Json(productos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en BuscarInventarioCompleto: {ex.Message}");
                return Json(new List<object>());
            }
        }

        private bool VentumExists(int id)
        {
            return _context.Venta.Any(e => e.IdVenta == id);
        }

        // GET: Venta/GetProductoInfo/5
        [HttpGet]
        public async Task<JsonResult> GetProductoInfo(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                return Json(new { 
                    id = producto.IdProducto,
                    precioVenta = producto.PrecioVenta, 
                    stock = producto.StockActual,
                    codigo = producto.Codigo,
                    nombre = producto.Nombre
                });
            }
            return Json(null);
        }

        // GET: Venta/BuscarProductos
        [HttpGet]
        public async Task<JsonResult> BuscarProductos(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            var productos = await _context.Productos
                .Where(p => p.Activo == true && p.StockActual > 0)
                .Where(p => p.Nombre.Contains(term) || 
                           (p.Descripcion != null && p.Descripcion.Contains(term)))
                .Select(p => new {
                    id = p.IdProducto,
                    nombre = p.Nombre,
                    descripcion = p.Descripcion,
                    codigo = p.Codigo,
                    precioVenta = p.PrecioVenta,
                    stock = p.StockActual
                })
                .Take(10)
                .ToListAsync();

            return Json(productos);
        }
    }
}
