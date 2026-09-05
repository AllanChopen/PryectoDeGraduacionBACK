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
    public DbSet<MensajeContacto> MensajesContacto => Set<MensajeContacto>();
    public DbSet<BandaStats> BandaStats => Set<BandaStats>();
    public DbSet<RedSocial> RedesSociales => Set<RedSocial>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTest>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable("dbtest");
            entity.Property(e => e.Prueba).HasColumnName("prueba");
        });

        modelBuilder.Entity<MensajeContacto>(entity =>
        {
            entity.ToTable("mensaje_contacto");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Uuid)
                .HasColumnName("uuid");

            entity.Property(e => e.BandaId)
                .HasColumnName("banda_id");

            entity.Property(e => e.Nombre)
                .HasColumnName("nombre");

            entity.Property(e => e.Email)
                .HasColumnName("email");

            entity.Property(e => e.Mensaje)
                .HasColumnName("mensaje");

            entity.Property(e => e.Estado)
                .HasColumnName("estado");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<BandaStats>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("banda_stats");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.Nombre)
                .HasColumnName("nombre");

            entity.Property(e => e.TotalSubscriptores)
                .HasColumnName("total_subscriptores");

            entity.Property(e => e.TotalProductos)
                .HasColumnName("total_productos");

            entity.Property(e => e.TotalEventos)
                .HasColumnName("total_eventos");

            entity.Property(e => e.TotalPublicaciones)
                .HasColumnName("total_publicaciones");

            entity.Property(e => e.TotalMensajes)
                .HasColumnName("total_mensajes");
        });
    }
}
