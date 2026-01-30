using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControlInventario.Models
{
    public partial class ControlFarmaclinicContext : DbContext
    {
        public ControlFarmaclinicContext(DbContextOptions<ControlFarmaclinicContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Caja> Cajas { get; set; }
        public virtual DbSet<DetalleVentum> DetalleVenta { get; set; }
        public virtual DbSet<MovimientoCaja> MovimientoCajas { get; set; }
        public virtual DbSet<MovimientoInventario> MovimientoInventarios { get; set; }
        public virtual DbSet<Producto> Productos { get; set; }
        public virtual DbSet<SaldoCajaDiario> SaldoCajaDiarios { get; set; }
        public virtual DbSet<Usuario> Usuarios { get; set; }
        public virtual DbSet<Ventum> Venta { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Caja>(entity =>
            {
                entity.HasKey(e => e.IdCaja);
                entity.Property(e => e.Activa).HasDefaultValue(true);
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(NOW())");
            });

            modelBuilder.Entity<DetalleVentum>(entity =>
            {
                entity.HasKey(e => e.IdDetalleVenta);
                entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetalleVenta)
                    .OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.DetalleVenta)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<MovimientoCaja>(entity =>
            {
                entity.HasKey(e => e.IdMovimientoCaja);
                entity.Property(e => e.Fecha).HasDefaultValueSql("(NOW())");
                entity.HasOne(d => d.IdCajaNavigation)
                    .WithMany(p => p.MovimientoCajas)
                    .HasForeignKey(d => d.IdCaja)
                    .OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasOne(d => d.IdUsuarioNavigation)
                    .WithMany(p => p.MovimientoCajas)
                    .HasForeignKey(d => d.IdUsuario)
                    .OnDelete(DeleteBehavior.ClientSetNull);
                
                // Índice para PostgreSQL
                entity.HasIndex(e => e.Fecha).HasDatabaseName("ix_movcaja_fecha");
            });

            modelBuilder.Entity<MovimientoInventario>(entity =>
            {
                entity.HasKey(e => e.IdMovimiento);
                entity.Property(e => e.Fecha).HasDefaultValueSql("(NOW())");
                entity.Property(e => e.TipoMovimiento).IsFixedLength();
                
                // Índice para PostgreSQL
                entity.HasIndex(e => e.IdProducto).HasDatabaseName("ix_movinv_producto");
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(e => e.IdProducto);
                entity.Property(e => e.Activo).HasDefaultValue(true);
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(NOW())");
                entity.Property(e => e.StockActual).HasDefaultValue(0);
                
                // Índices para PostgreSQL
                entity.HasIndex(e => e.Codigo).HasDatabaseName("ix_producto_codigo");
                entity.HasIndex(e => e.Codigo).IsUnique().HasDatabaseName("producto_codigo_key");
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.IdUsuario);
                entity.Property(e => e.Activo).HasDefaultValue(true);
                entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(NOW())");
                entity.Property(e => e.Usuario1).HasColumnName("usuario").HasMaxLength(50).IsRequired();
                
                // Índice único para PostgreSQL
                entity.HasIndex(e => e.Usuario1).IsUnique().HasDatabaseName("usuario_usuario_key");
            });

            modelBuilder.Entity<Ventum>(entity =>
            {
                entity.HasKey(e => e.IdVenta);
                entity.Property(e => e.Anulada).HasDefaultValue(false);
                entity.Property(e => e.Fecha).HasDefaultValueSql("(NOW())");
                
                // Índices para PostgreSQL
                entity.HasIndex(e => e.Fecha).HasDatabaseName("ix_venta_fecha");
                entity.HasIndex(e => e.NumeroVenta).IsUnique().HasDatabaseName("venta_numero_venta_key");
            });

            modelBuilder.Entity<SaldoCajaDiario>(entity =>
            {
                entity.HasKey(e => e.IdSaldoCajaDiario);
                entity.Property(e => e.Cerrado).HasDefaultValue(false);
                entity.Property(e => e.FechaCierre).HasDefaultValueSql("(NOW())");
                entity.HasOne(d => d.IdCajaNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.IdCaja)
                    .OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasOne(d => d.IdUsuarioCierreNavigation)
                    .WithMany()
                    .HasForeignKey(d => d.IdUsuarioCierre)
                    .OnDelete(DeleteBehavior.ClientSetNull);
                
                // Índice único para PostgreSQL
                entity.HasIndex(e => new { e.Fecha, e.IdCaja }).IsUnique().HasDatabaseName("ux_saldo_caja_fecha_caja");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
        
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Configurar DateTime para PostgreSQL
            configurationBuilder.Properties<DateTime>()
                .HaveConversion(typeof(DateTimeToUtcConverter));
        }
    }
    
    // Convertidor personalizado para DateTime UTC
    public class DateTimeToUtcConverter : ValueConverter<DateTime, DateTime>
    {
        public DateTimeToUtcConverter()
            : base(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}
