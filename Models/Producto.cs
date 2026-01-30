using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Models;

[Table("producto")]
public partial class Producto
{
    [Key]
    [Column("id_producto")]
    public int IdProducto { get; set; }

    [Column("codigo")]
    [StringLength(50)]
    public string Codigo { get; set; } = null!;

    [Column("nombre")]
    [StringLength(150)]
    public string Nombre { get; set; } = null!;

    [Column("descripcion")]
    [StringLength(255)]
    public string? Descripcion { get; set; }

    [Column("precio_ingreso", TypeName = "numeric(10,2)")]
    public decimal PrecioIngreso { get; set; }

    [Column("precio_venta", TypeName = "numeric(10,2)")]
    public decimal PrecioVenta { get; set; }

    [Column("stock_actual")]
    public int StockActual { get; set; }

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("fecha_creacion")]
    public DateTime? FechaCreacion { get; set; }

    [InverseProperty("IdProductoNavigation")]
    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    [InverseProperty("IdProductoNavigation")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();
}
