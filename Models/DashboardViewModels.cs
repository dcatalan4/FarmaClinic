using System;
using System.Collections.Generic;

namespace ControlInventario.Models
{
    public class DashboardAdminViewModel
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

        // Ventas semana
        public int VentasSemana { get; set; }
        public decimal VentasSemanaMonto { get; set; }

        // Caja
        public decimal SaldoCajaPrincipal { get; set; }
        public int CajasActivas { get; set; }

        // Usuarios
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }

        // Productos más vendidos (ahora 10)
        public List<ProductoTop> ProductosMasVendidos { get; set; } = new();

        // Ventas recientes
        public List<VentaReciente> VentasRecientes { get; set; } = new();

        // Métricas adicionales
        public decimal TicketPromedio { get; set; }
        public int ProductosSinStock { get; set; }
        public decimal CrecimientoVentasMes { get; set; }
    }

    public class DashboardVendedorViewModel
    {
        // Ventas personales
        public int MisVentasHoy { get; set; }
        public int MisVentasMes { get; set; }
        public decimal MisVentasHoyMonto { get; set; }
        public decimal MisVentasMesMonto { get; set; }

        // Ventas semana personales
        public int MisVentasSemana { get; set; }
        public decimal MisVentasSemanaMonto { get; set; }

        // Productos
        public int ProductosActivos { get; set; }
        public int ProductosBajoStock { get; set; }

        // Caja
        public decimal SaldoCajaPrincipal { get; set; }

        // Mis productos más vendidos (ahora 10)
        public List<ProductoTop> MisProductosMasVendidos { get; set; } = new();

        // Mis ventas recientes
        public List<VentaReciente> MisVentasRecientes { get; set; } = new();

        // Información del usuario
        public string NombreUsuario { get; set; } = "";
        public string RolUsuario { get; set; } = "";

        // Métricas adicionales personales
        public decimal MiTicketPromedio { get; set; }
        public int MisProductosSinStock { get; set; }
        public decimal MiCrecimientoVentasMes { get; set; }
    }
}
