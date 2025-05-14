using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AppiNon.Models;

public partial class PinonBdContext : DbContext
{
    public PinonBdContext(){}

    public PinonBdContext(DbContextOptions<PinonBdContext> options)
        : base(options){}

    public virtual DbSet<Usuarios> Usuarios { get; set; }
    public virtual DbSet<Inventario> Inv { get; set; }
    public virtual DbSet<Roles> Roles { get; set; }
    public virtual DbSet<Categorias> Categorias { get; set; }
    public virtual DbSet<Proveedores> Proveedores { get; set; }
    public virtual DbSet<Producto> Producto { get; set; }
    public virtual DbSet<Pedido> Pedidos { get; set; }
    public virtual DbSet<Bitacora> Bitacora { get; set; }

    public DbSet<Predicciones> Predicciones { get; set; }
    public DbSet<ParametrosSistema> ParametrosSistema { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LUIS; DataBase=PinonBD;Integrated Security=true; TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categorias>(entity =>  {
            entity.HasKey(e => e.id_categoria);
            entity.Property(e => e.Categoria).HasMaxLength(255);
        });

        modelBuilder.Entity<Proveedores>(entity =>
        {
            entity.HasKey(e => e.ID_proveedor);
            entity.Property(e => e.Nombre_Proveedor).HasMaxLength(255);
            entity.Property(e => e.Tiempo_entrega_dias).HasColumnType("int"); // Cambiado a int
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.ID);
            entity.Property(e => e.Nombre_rol).HasMaxLength(255);
            entity.Property(e => e.Descripcion).HasMaxLength(255);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.Id_producto);
            entity.Property(e => e.Nombre_producto).HasMaxLength(255);
            entity.Property(e => e.Unidad_medida).HasMaxLength(255);
            entity.Property(e => e.Id_categoria).HasColumnType("int");
            entity.Property(e => e.Metodoprediccion).HasMaxLength(255);

            entity.Property(e => e.Reabastecimientoautomatico)
                  .HasDefaultValue(true); // Valor por defecto

            entity.HasOne<Categorias>()
                  .WithMany()
                  .HasForeignKey(e => e.Id_categoria);



            entity.HasOne<Proveedores>()
                  .WithMany()
                  .HasForeignKey(e => e.Id_provedor)
                  .HasConstraintName("FK_Producto_Provedores");
        });


        // Configuración de Pedidos
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.IdPedido);

            entity.Property(e => e.Estado)
                  .HasMaxLength(20)
                  .HasDefaultValue("Pendiente");

            entity.Property(e => e.FechaSolicitud)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.FechaRecepcion)
                  .HasColumnType("datetime");

            // Relaciones
            entity.HasOne(p => p.Producto)
                  .WithMany()
                  .HasForeignKey(p => p.IdProducto)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Proveedor)
                  .WithMany()
                  .HasForeignKey(p => p.IdProveedor)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Inventario>(entity =>
        {
            entity.HasKey(e => e.IdInventario);
            entity.Property(e => e.IdProducto).HasColumnType("int");
            entity.Property(e => e.StockActual).HasColumnType("int");
            entity.Property(e => e.StockMinimo).HasColumnType("int");
            entity.Property(e => e.StockIdeal).HasColumnType("int");
            entity.Property(e => e.UltimaEntrada).HasColumnType("datetime");

            
            entity.HasOne(e => e.Producto)
                  .WithOne(p => p.Inventario)
                  .HasForeignKey<Inventario>(e => e.IdProducto);
        });



        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.ID);

            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Correo).HasMaxLength(100);
            entity.Property(e => e.Contraseña_hash).HasMaxLength(255);
            entity.Property(e => e.Rol_id).HasColumnType("int"); 
            // Relación con Roles
            entity.HasOne<Roles>()
                .WithMany()
                .HasForeignKey(e => e.Rol_id);
        });

        modelBuilder.Entity<Bitacora>(entity =>
        {
            entity.HasKey(e => e.ID);
            // Relación con Usuarios (FALTABA)
            entity.HasOne<Usuarios>()
                .WithMany()
                .HasForeignKey(e => e.ID_Usuario);
        });


        // Configuración para Predicciones
        modelBuilder.Entity<Predicciones>(entity =>
        {
            entity.HasKey(e => e.id_prediccion);

            entity.Property(e => e.MetodoUsado)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.ConsumoPredicho)
                .HasColumnType("decimal(10,2)");

            entity.HasOne(p => p.Producto)
                .WithMany()
                .HasForeignKey(p => p.id_producto)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParametrosSistema>(entity =>
        {
            entity.HasKey(e => e.id_parametro);

            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Valor)
                .HasColumnType("decimal(10,2)");

            entity.HasIndex(e => e.Nombre)
                .IsUnique();
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
