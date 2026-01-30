using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Models;

[Table("movimiento_inventario")]
public partial class MovimientoInventario
{
    [Key]
    [Column("id_movimiento")]
    public int IdMovimiento { get; set; }

    [Column("id_producto")]
    public int IdProducto { get; set; }

    [Column("tipo_movimiento")]
    [StringLength(1)]
    public string TipoMovimiento { get; set; } = null!;

    [Column("cantidad")]
    public int Cantidad { get; set; }

    [Column("fecha")]
    public DateTime? Fecha { get; set; }

    [Column("referencia")]
    [StringLength(50)]
    public string? Referencia { get; set; }

    [Column("id_referencia")]
    public int? IdReferencia { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [ForeignKey("IdProducto")]
    [InverseProperty("MovimientoInventarios")]
    public virtual Producto IdProductoNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("MovimientoInventarios")]
    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
