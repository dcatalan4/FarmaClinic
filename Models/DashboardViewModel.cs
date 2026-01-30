using System;
using System.Collections.Generic;

namespace ControlInventario.Models
{
    public class DashboardViewModel
    {
        // Productos
        public int TotalProductos { get; set; }
        public int ProductosActivos { get; set; }
        public int ProductosBajoStock { get; set; }
        public decimal ValorTotalInventario { get; set; }

        // Ventas
        public int VentasHoy { get; set; }
        public int VentasMes { get; set; }
        public decimal VentasHoyMonto { get; set; }
        public decimal VentasMesMonto { get; set; }

        // Caja
        public decimal SaldoCajaPrincipal { get; set; }
        public int CajasActivas { get; set; }

        // Productos más vendidos
        public List<ProductoTop> ProductosMasVendidos { get; set; } = new();

        // Ventas recientes
        public List<VentaReciente> VentasRecientes { get; set; } = new();
    }

    public class ProductoTop
    {
        public string Nombre { get; set; } = "";
        public int CantidadVendida { get; set; }
        public decimal TotalVendido { get; set; }
    }

    public class VentaReciente
    {
        public string NumeroVenta { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string UsuarioNombre { get; set; } = "";
    }
}
