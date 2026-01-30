using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Models;

namespace ControlInventario.Controllers
{
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly ControlFarmaclinicContext _context;

        public ProductoController(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        // GET: Producto
        [Authorize(Roles = "Admin,Vendedor")]
        public async Task<IActionResult> Index(string busqueda = "", int page = 1)
        {
            int pageSize = 10;
            var query = _context.Productos.AsQueryable();

            // Aplicar filtro de búsqueda (case-insensitive)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query = query.Where(p => 
                    EF.Functions.Like(p.Codigo.ToLower(), $"%{busqueda.ToLower()}%") ||
                    EF.Functions.Like(p.Nombre.ToLower(), $"%{busqueda.ToLower()}%") ||
                    (p.Descripcion != null && EF.Functions.Like(p.Descripcion.ToLower(), $"%{busqueda.ToLower()}%"))
                );
            }

            // Contar total de productos
            int totalProductos = await query.CountAsync();

            // Aplicar paginación
            var productos = await query
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag para paginación y búsqueda
            ViewBag.CurrentBusqueda = busqueda;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProductos / pageSize);
            ViewBag.TotalProductos = totalProductos;

            return View(productos);
        }

        // GET: Producto/Details/5
        [Authorize(Roles = "Admin,Vendedor")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: Producto/Create
        [Authorize(Roles = "Admin,Vendedor")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Producto/Create
        [Authorize(Roles = "Admin,Vendedor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdProducto,Codigo,Nombre,Descripcion,PrecioIngreso,PrecioVenta,StockActual,Activo,FechaCreacion")] Producto producto)
        {
            try
            {
                // Validar que el código no exista
                var codigoExistente = await _context.Productos
                    .AnyAsync(p => p.Codigo == producto.Codigo && p.IdProducto != producto.IdProducto);
                
                if (codigoExistente)
                {
                    ModelState.AddModelError("Codigo", "El código de producto ya existe.");
                }

                if (ModelState.IsValid)
                {
                    producto.FechaCreacion = DateTime.Now;
                    producto.Activo = true;
                    
                    _context.Add(producto);
                    await _context.SaveChangesAsync();
                    
                    // Obtener un usuario válido para el movimiento
                    var usuarioId = await ObtenerUsuarioValido();
                    
                    // Crear movimiento de inventario inicial solo si hay stock
                    if (producto.StockActual > 0)
                    {
                        try
                        {
                            var movimiento = new MovimientoInventario
                            {
                                IdProducto = producto.IdProducto,
                                TipoMovimiento = "E", // E para Entrada (según CHECK constraint)
                                Cantidad = producto.StockActual,
                                Fecha = DateTime.Now,
                                Referencia = "Stock inicial",
                                IdUsuario = usuarioId
                            };
                            
                            _context.Add(movimiento);
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception movEx)
                        {
                            // Si falla el movimiento, loguear pero continuar
                            Console.WriteLine($"Error al crear movimiento de inventario: {movEx.Message}");
                            // No fallar toda la operación por el movimiento
                        }
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al crear el producto: {ex.Message}");
                Console.WriteLine($"Error en Create: {ex.Message}");
            }
            
            return View(producto);
        }

        // GET: Producto/Edit/5
        [Authorize(Roles = "Admin,Vendedor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            return View(producto);
        }

        // POST: Producto/Edit/5
        [Authorize(Roles = "Admin,Vendedor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProducto,Codigo,Nombre,Descripcion,PrecioIngreso,PrecioVenta,StockActual,Activo,FechaCreacion")] Producto producto)
        {
            if (id != producto.IdProducto)
            {
                return NotFound();
            }

            // Validar que el código no exista (excepto para este mismo producto)
            var codigoExistente = await _context.Productos
                .AnyAsync(p => p.Codigo == producto.Codigo && p.IdProducto != producto.IdProducto);
            
            if (codigoExistente)
            {
                ModelState.AddModelError("Codigo", "El código de producto ya existe.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var productoOriginal = await _context.Productos.FindAsync(id);
                    if (productoOriginal == null)
                    {
                        return NotFound();
                    }

                    // Verificar si cambió el stock para crear movimiento
                    if (productoOriginal.StockActual != producto.StockActual)
                    {
                        try
                        {
                            var usuarioId = await ObtenerUsuarioValido();
                            
                            var movimiento = new MovimientoInventario
                            {
                                IdProducto = producto.IdProducto,
                                TipoMovimiento = producto.StockActual > productoOriginal.StockActual ? "E" : "S", // E para Entrada, S para Salida
                                Cantidad = Math.Abs(producto.StockActual - productoOriginal.StockActual),
                                Fecha = DateTime.Now,
                                Referencia = "Ajuste de stock",
                                IdUsuario = usuarioId
                            };
                            
                            _context.Add(movimiento);
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception movEx)
                        {
                            // Si falla el movimiento, loguear pero continuar
                            Console.WriteLine($"Error al crear movimiento de inventario: {movEx.Message}");
                            // No fallar toda la operación por el movimiento
                        }
                    }

                    // Actualizar propiedades
                    productoOriginal.Codigo = producto.Codigo;
                    productoOriginal.Nombre = producto.Nombre;
                    productoOriginal.Descripcion = producto.Descripcion;
                    productoOriginal.PrecioIngreso = producto.PrecioIngreso;
                    productoOriginal.PrecioVenta = producto.PrecioVenta;
                    productoOriginal.StockActual = producto.StockActual;
                    productoOriginal.Activo = producto.Activo;

                    _context.Update(productoOriginal);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.IdProducto))
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
            return View(producto);
        }

        // GET: Producto/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .FirstOrDefaultAsync(m => m.IdProducto == id);
            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Producto/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                // En lugar de eliminar, marcar como inactivo
                producto.Activo = false;
                _context.Update(producto);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Producto/Activar/5
        public async Task<IActionResult> Activar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            producto.Activo = true;
            _context.Update(producto);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Producto/Movimientos/5
        public async Task<IActionResult> Movimientos(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            var movimientos = await _context.MovimientoInventarios
                .Where(m => m.IdProducto == id)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            ViewBag.Producto = producto;
            return View(movimientos);
        }

        // GET: Producto/Ingresar
        public IActionResult Ingresar()
        {
            return View();
        }

        // POST: Producto/BuscarProducto
        [HttpPost]
        public async Task<JsonResult> BuscarProducto(string termino)
        {
            try
            {
                var productos = await _context.Productos
                    .Where(p => p.Activo == true && (
                        EF.Functions.Like(p.Codigo.ToLower(), $"%{termino.ToLower()}%") ||
                        EF.Functions.Like(p.Nombre.ToLower(), $"%{termino.ToLower()}%") ||
                        (p.Descripcion != null && EF.Functions.Like(p.Descripcion.ToLower(), $"%{termino.ToLower()}%"))
                    ))
                    .OrderBy(p => p.Nombre)
                    .Select(p => new
                    {
                        idProducto = p.IdProducto,
                        codigo = p.Codigo,
                        nombre = p.Nombre,
                        descripcion = p.Descripcion,
                        precioIngreso = p.PrecioIngreso,
                        precioVenta = p.PrecioVenta,
                        stockActual = p.StockActual
                    })
                    .Take(20)
                    .ToListAsync();

                return Json(productos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en BuscarProducto: {ex.Message}");
                return Json(new List<object>());
            }
        }

        // POST: Producto/ProcesarIngreso
        [HttpPost]
        public async Task<IActionResult> ProcesarIngreso([FromBody] List<IngresoProducto> productos)
        {
            try
            {
                Console.WriteLine("Recibiendo solicitud de procesar ingreso...");
                
                if (productos == null || !productos.Any())
                {
                    Console.WriteLine("Error: No hay productos para procesar");
                    return Json(new { success = false, message = "No hay productos para procesar" });
                }

                Console.WriteLine($"Procesando {productos.Count} productos...");

                var movimientos = new List<MovimientoInventario>();
                var usuarioId = await ObtenerUsuarioValido();
                Console.WriteLine($"Usuario obtenido: {usuarioId}");

                foreach (var item in productos)
                {
                    Console.WriteLine($"Procesando producto: {item.Nombre}, Cantidad: {item.Cantidad}, ID: {item.IdProducto}");
                    
                    if (item.Cantidad <= 0)
                    {
                        Console.WriteLine($"Error: Cantidad inválida para {item.Nombre}");
                        return Json(new { success = false, message = $"La cantidad para {item.Nombre} debe ser mayor a cero" });
                    }

                    if (item.IdProducto == 0)
                    {
                        // Verificar si ya existe un producto con el mismo código
                        var codigoExistente = await _context.Productos
                            .AnyAsync(p => p.Codigo == item.Codigo);
                        
                        if (codigoExistente)
                        {
                            return Json(new { success = false, message = $"El código {item.Codigo} ya existe en la base de datos" });
                        }

                        // Crear nuevo producto
                        Console.WriteLine($"Creando nuevo producto: {item.Nombre}");
                        var nuevoProducto = new Producto
                        {
                            Codigo = item.Codigo,
                            Nombre = item.Nombre,
                            Descripcion = item.Descripcion ?? "",
                            PrecioIngreso = item.PrecioIngreso,
                            PrecioVenta = item.PrecioVenta,
                            StockActual = item.Cantidad,
                            Activo = true,
                            FechaCreacion = DateTime.Now
                        };

                        _context.Productos.Add(nuevoProducto);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Nuevo producto creado con ID: {nuevoProducto.IdProducto}");

                        // Crear movimiento de inventario solo si hay stock
                        if (nuevoProducto.StockActual > 0)
                        {
                            Console.WriteLine($"Creando movimiento para producto {nuevoProducto.IdProducto}");
                            Console.WriteLine($"Datos del movimiento: ProductoID={nuevoProducto.IdProducto}, Tipo=E, Cantidad={nuevoProducto.StockActual}, UsuarioID={usuarioId}");
                            
                            var movimiento = new MovimientoInventario
                            {
                                IdProducto = nuevoProducto.IdProducto,
                                TipoMovimiento = "E", // E para Entrada (según CHECK constraint)
                                Cantidad = nuevoProducto.StockActual,
                                Fecha = DateTime.Now,
                                Referencia = "Carga al inventario",
                                IdUsuario = usuarioId
                            };
                            movimientos.Add(movimiento);
                            Console.WriteLine($"Movimiento agregado a la lista. Total movimientos: {movimientos.Count}");
                        }
                        else
                        {
                            Console.WriteLine($"No se crea movimiento porque el stock es 0 o menor: {nuevoProducto.StockActual}");
                        }
                    }
                    else
                    {
                        // Actualizar producto existente
                        Console.WriteLine($"Actualizando producto existente: {item.Nombre}");
                        var producto = await _context.Productos.FindAsync(item.IdProducto);
                        if (producto != null)
                        {
                            var stockAnterior = producto.StockActual;
                            producto.StockActual += item.Cantidad;
                            Console.WriteLine($"Stock actualizado: {stockAnterior} -> {producto.StockActual}");

                            // Crear movimiento de inventario solo si hay cantidad
                            if (item.Cantidad > 0)
                            {
                                Console.WriteLine($"Creando movimiento para producto existente {producto.IdProducto}");
                                Console.WriteLine($"Datos del movimiento: ProductoID={producto.IdProducto}, Tipo=E, Cantidad={item.Cantidad}, UsuarioID={usuarioId}");
                                
                                var movimiento = new MovimientoInventario
                                {
                                    IdProducto = producto.IdProducto,
                                    TipoMovimiento = "E", // E para Entrada (según CHECK constraint)
                                    Cantidad = item.Cantidad,
                                    Fecha = DateTime.Now,
                                    Referencia = "Carga al inventario",
                                    IdUsuario = usuarioId
                                };
                                movimientos.Add(movimiento);
                                Console.WriteLine($"Movimiento agregado a la lista. Total movimientos: {movimientos.Count}");
                            }
                            else
                            {
                                Console.WriteLine($"No se crea movimiento porque la cantidad es 0 o menor: {item.Cantidad}");
                            }

                            _context.Update(producto);
                            await _context.SaveChangesAsync(); // Guardar cambios del producto
                            Console.WriteLine($"Producto actualizado en base de datos");
                        }
                        else
                        {
                            Console.WriteLine($"Error: No se encontró el producto con ID {item.IdProducto}");
                            return Json(new { success = false, message = $"No se encontró el producto con ID {item.IdProducto}" });
                        }
                    }
                }

            // Guardar todos los movimientos con manejo de errores
            Console.WriteLine($"Procesando {movimientos.Count} movimientos para guardar");
            if (movimientos.Any())
            {
                try
                {
                    _context.AddRange(movimientos);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Movimientos guardados exitosamente");
                    
                    // Verificación adicional: contar movimientos en la BD
                    var totalMovimientos = await _context.MovimientoInventarios.CountAsync();
                    Console.WriteLine($"Total de movimientos en la base de datos: {totalMovimientos}");
                    
                    // Mostrar detalles del último movimiento guardado
                    var ultimoMovimiento = await _context.MovimientoInventarios
                        .Include(m => m.IdProductoNavigation)
                        .OrderByDescending(m => m.IdMovimiento)
                        .FirstOrDefaultAsync();
                    
                    if (ultimoMovimiento != null)
                    {
                        Console.WriteLine($"Último movimiento guardado: Producto={ultimoMovimiento.IdProductoNavigation?.Nombre}, Tipo={ultimoMovimiento.TipoMovimiento}, Cantidad={ultimoMovimiento.Cantidad}, Fecha={ultimoMovimiento.Fecha}");
                    }
                }
                catch (Exception movEx)
                {
                    Console.WriteLine($"Error al guardar movimientos: {movEx.Message}");
                    Console.WriteLine($"Stack trace: {movEx.StackTrace}");
                    // No fallar toda la operación por los movimientos
                }
            }
            else
            {
                Console.WriteLine("No hay movimientos para guardar");
            }

            Console.WriteLine("Ingreso procesado correctamente");
            return Json(new { success = true, message = "Importación completada correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error en la importación: " + ex.Message });
            }
        }

        private async Task<int> ObtenerUsuarioValido()
        {
            try
            {
                Console.WriteLine("Buscando usuario válido...");
                
                // Intentar obtener cualquier usuario existente
                var usuarioExistente = await _context.Usuarios.FirstOrDefaultAsync();
                if (usuarioExistente != null)
                {
                    Console.WriteLine($"Usuario existente encontrado: ID={usuarioExistente.IdUsuario}, Usuario={usuarioExistente.Usuario1}");
                    return usuarioExistente.IdUsuario;
                }
                
                Console.WriteLine("No se encontraron usuarios existentes, creando usuario por defecto...");
                
                // Si no hay usuarios, crear uno por defecto con nombre único
                var timestamp = DateTime.Now.Ticks;
                var usuarioDefecto = new Usuario
                {
                    Usuario1 = $"admin_{timestamp}",
                    PasswordHash = "temporal123",
                    Nombre = "Administrador",
                    Rol = "Admin",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
                
                _context.Usuarios.Add(usuarioDefecto);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"Usuario por defecto creado: ID={usuarioDefecto.IdUsuario}, Usuario={usuarioDefecto.Usuario1}");
                return usuarioDefecto.IdUsuario;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ObtenerUsuarioValido: {ex.Message}");
                // Si todo falla, intentar buscar cualquier usuario o retornar 1
                try
                {
                    var usuario = await _context.Usuarios.FindAsync(1);
                    if (usuario != null)
                    {
                        Console.WriteLine($"Usuario fallback encontrado: ID={usuario.IdUsuario}");
                        return usuario.IdUsuario;
                    }
                }
                catch
                {
                    Console.WriteLine("Fallback también falló, retornando 1");
                }
                return 1;
            }
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.IdProducto == id);
        }
    }

    public class IngresoProducto
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public decimal PrecioIngreso { get; set; }
        public decimal PrecioVenta { get; set; }
        public int Cantidad { get; set; }
    }
}
