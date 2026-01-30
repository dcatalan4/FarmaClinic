using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Models;

[Table("detalle_venta")]
public partial class DetalleVentum
{
    [Key]
    [Column("id_detalle_venta")]
    public int IdDetalleVenta { get; set; }

    [Column("id_venta")]
    public int IdVenta { get; set; }

    [Column("id_producto")]
    public int IdProducto { get; set; }

    [Column("cantidad")]
    public int Cantidad { get; set; }

    [Column("precio_unitario", TypeName = "numeric(10,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column("precio_ingreso", TypeName = "numeric(10,2)")]
    public decimal PrecioIngreso { get; set; }

    [Column("subtotal", TypeName = "numeric(10,2)")]
    public decimal Subtotal { get; set; }

    [ForeignKey("IdProducto")]
    [InverseProperty("DetalleVenta")]
    public virtual Producto IdProductoNavigation { get; set; } = null!;

    [ForeignKey("IdVenta")]
    [InverseProperty("DetalleVenta")]
    public virtual Ventum IdVentaNavigation { get; set; } = null!;
}
