using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Models;

[Table("movimiento_caja")]
public partial class MovimientoCaja
{
    [Key]
    [Column("id_movimiento_caja")]
    public int IdMovimientoCaja { get; set; }

    [Column("id_caja")]
    public int IdCaja { get; set; }

    [Column("tipo_movimiento")]
    [StringLength(1)]
    public string TipoMovimiento { get; set; } = null!;

    [Column("monto", TypeName = "numeric(10,2)")]
    public decimal Monto { get; set; }

    [Column("fecha")]
    public DateTime? Fecha { get; set; }

    [Column("concepto")]
    [StringLength(50)]
    public string? Concepto { get; set; }

    [Column("id_referencia")]
    public int? IdReferencia { get; set; }

    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("saldo_en_momento", TypeName = "numeric(10,2)")]
    public decimal SaldoEnMomento { get; set; }

    [ForeignKey("IdCaja")]
    [InverseProperty("MovimientoCajas")]
    public virtual Caja IdCajaNavigation { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    [InverseProperty("MovimientoCajas")]
    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
