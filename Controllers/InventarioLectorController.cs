using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlInventario.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ControlInventario.Controllers
{
    [Authorize(Policy = "LectorPolicy")]
    public class InventarioLectorController : Controller
    {
        private readonly ControlFarmaclinicContext _context;

        public InventarioLectorController(ControlFarmaclinicContext context)
        {
            _context = context;
        }

        // GET: InventarioLector
        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
            
            return View(productos);
        }

        // GET: InventarioLector/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.MovimientoInventarios)
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: InventarioLector/Movimientos/5
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
                .Include(m => m.IdUsuarioNavigation)
                .Where(m => m.IdProducto == id)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();

            ViewBag.Producto = producto;
            return View(movimientos);
        }
    }
}
