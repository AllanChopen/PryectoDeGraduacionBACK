using Microsoft.EntityFrameworkCore;
using Proyecto;
using Proyecto.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DbTest> DbTests => Set<DbTest>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Banda> Bandas => Set<Banda>();
    public DbSet<Subscriptor> Subscriptores => Set<Subscriptor>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Orden> Ordenes => Set<Orden>();
    public DbSet<OrdenDetalle> OrdenesDetalle => Set<OrdenDetalle>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Publicacion> Publicaciones => Set<Publicacion>();
    public DbSet<ProductoVariacion> ProductoVariaciones => Set<ProductoVariacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTest>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("dbtest");
            entity.Property(e => e.Prueba).HasColumnName("prueba");
        });
    }
}
