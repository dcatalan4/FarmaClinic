using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Models;

[Table("caja")]
public partial class Caja
{
    [Key]
    [Column("id_caja")]
    public int IdCaja { get; set; }

    [Column("nombre")]
    [StringLength(50)]
    public string Nombre { get; set; } = null!;

    [Column("saldo_actual", TypeName = "numeric(10,2)")]
    public decimal SaldoActual { get; set; }

    [Column("activa")]
    public bool Activa { get; set; }

    [Column("fecha_creacion")]
    public DateTime? FechaCreacion { get; set; }

    [InverseProperty("IdCajaNavigation")]
    public virtual ICollection<MovimientoCaja> MovimientoCajas { get; set; } = new List<MovimientoCaja>();
}
