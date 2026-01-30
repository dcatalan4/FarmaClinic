using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ControlInventario.Models;

[Table("usuario")]
public partial class Usuario
{
    [Key]
    [Column("id_usuario")]
    public int IdUsuario { get; set; }

    [Column("usuario")]
    [StringLength(50)]
    public string Usuario1 { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Column("nombre")]
    [StringLength(100)]
    public string? Nombre { get; set; }

    [Column("rol")]
    [StringLength(20)]
    public string Rol { get; set; } = null!;

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("fecha_creacion")]
    public DateTime? FechaCreacion { get; set; }

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<MovimientoCaja> MovimientoCajas { get; set; } = new List<MovimientoCaja>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();

    [InverseProperty("IdUsuarioNavigation")]
    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
