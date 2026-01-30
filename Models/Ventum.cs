using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Models;

[Table("venta")]
public partial class Ventum
{
    [Key]
    [Column("id_venta")]
    public int IdVenta { get; set; }

    [Column("numero_venta")]
    public int NumeroVenta { get; set; }

    [Column("fecha")]
    public DateTime? Fecha { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("subtotal", TypeName = "numeric(10,2)")]
    public decimal Subtotal { get; set; }

    [Column("total", TypeName = "numeric(10,2)")]
    public decimal Total { get; set; }

    [Column("anulada")]
    public bool Anulada { get; set; }

    [Column("activa")]
    public bool Activa { get; set; }

    [InverseProperty("IdVentaNavigation")]
    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    [ForeignKey("IdUsuario")]
    [InverseProperty("Venta")]
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
