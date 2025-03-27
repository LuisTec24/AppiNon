using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppiNon.Models;

public partial class PinonBdContext : DbContext
{
    public PinonBdContext()
    {
    }

    public PinonBdContext(DbContextOptions<PinonBdContext> options)
        : base(options)
    {
    }

    public virtual DbSet<GomezTable> GomezTables { get; set; }

    public virtual DbSet<LuisTable> LuisTables { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-O36R4PE\\SQLEXPRESS; DataBase=PinonBD;Integrated Security=true; TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
