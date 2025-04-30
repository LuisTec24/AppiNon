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

    public virtual DbSet<GomezTable> GomezTables { get; set; }
    public virtual DbSet<LuisTable> LuisTables { get; set; }
    public virtual DbSet<Usuarios> Usuarios { get; set; }
    public virtual DbSet<Producto> Producto { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LUIS; DataBase=PinonBD;Integrated Security=true; TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        /// Usuarios
        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PK__Usuarios__3213E83F3EE9E471");
            entity.ToTable("Usuarios");
            entity.Property(e => e.nombre).HasMaxLength(100);
            entity.Property(e => e.correo).HasMaxLength(100);
            entity.Property(e => e.contraseña_hash).HasMaxLength(255);
            entity.Property(e => e.rol_id).HasMaxLength(255);

        });
        /// Productos
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.id_producto).HasName("PK__inventar__FF341C0DCFF70F53");
            entity.ToTable("Producto");
            entity.Property(e => e.id_categoria).HasMaxLength(255);
            entity.Property(e => e.nombre_producto).HasMaxLength(255);
            entity.Property(e => e.unidad_medida).HasMaxLength(255);
        });

        modelBuilder.Entity<Inventario>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PK_inv");
            entity.ToTable("inv");
//            entity.Property(e => e.StockActual).HasMaxLength(255);
            entity.Property(e => e.StockActual).HasMaxLength(255);
            entity.Property(e => e.StockIdeal).HasMaxLength(255);
            entity.Property(e => e.StockMinimo).HasMaxLength(255);
            entity.Property(e => e.UltimaEntrada).HasMaxLength(255);

        });


       


        //Extra
        modelBuilder.Entity<GomezTable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__GomezTab__3214EC07C9EE7BB3");

            entity.ToTable("GomezTable");

            entity.Property(e => e.Clave).HasMaxLength(255);
            entity.Property(e => e.EsActivo).HasMaxLength(10);
            entity.Property(e => e.Nombre).HasMaxLength(255);
            entity.Property(e => e.Ocupacion).HasMaxLength(255);
            entity.Property(e => e.Ubicacion).HasMaxLength(255);
        });
        modelBuilder.Entity<LuisTable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LuisTabl__3214EC07D9FC37EC");

            entity.ToTable("LuisTable");

            entity.Property(e => e.Clave).HasMaxLength(255);
            entity.Property(e => e.EsActivo).HasMaxLength(10);
            entity.Property(e => e.Nombre).HasMaxLength(255);
            entity.Property(e => e.Ocupacion).HasMaxLength(255);
            entity.Property(e => e.Ubicacion).HasMaxLength(255);
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
