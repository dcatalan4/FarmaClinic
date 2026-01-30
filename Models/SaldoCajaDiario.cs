using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlInventario.Models;

[Table("saldo_caja_diario")]
public partial class SaldoCajaDiario
{
    [Key]
    [Column("id_saldo_caja_diario")]
    public int IdSaldoCajaDiario { get; set; }

    [Column("id_caja")]
    public int IdCaja { get; set; }

    [Column("fecha", TypeName = "date")]
    public DateTime Fecha { get; set; }

    [Column("saldo_inicial", TypeName = "numeric(10,2)")]
    public decimal SaldoInicial { get; set; }

    [Column("saldo_final", TypeName = "numeric(10,2)")]
    public decimal SaldoFinal { get; set; }

    [Column("total_ingresos", TypeName = "numeric(10,2)")]
    public decimal TotalIngresos { get; set; }

    [Column("total_egresos", TypeName = "numeric(10,2)")]
    public decimal TotalEgresos { get; set; }

    [Column("fecha_cierre")]
    public DateTime? FechaCierre { get; set; }

    [Column("id_usuario_cierre")]
    public int IdUsuarioCierre { get; set; }

    [Column("cerrado")]
    public bool Cerrado { get; set; } = false;

    [ForeignKey("IdCaja")]
    public virtual Caja IdCajaNavigation { get; set; } = null!;

    [ForeignKey("IdUsuarioCierre")]
    public virtual Usuario IdUsuarioCierreNavigation { get; set; } = null!;
}
