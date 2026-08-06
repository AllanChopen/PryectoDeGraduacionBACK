using Microsoft.EntityFrameworkCore;
using Proyecto;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DbTest> DbTests => Set<DbTest>();

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
