using Microsoft.EntityFrameworkCore;

namespace TemporalDemoApi.Model
{
    public class TemporalDbContext : DbContext
    {
        public TemporalDbContext(DbContextOptions<TemporalDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Author { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>()
                .ToTable("Book", c => c.IsTemporal());

            modelBuilder.Entity<Author>()
                .ToTable("Author", c => c.IsTemporal());

            modelBuilder.Entity<Author>()
                .HasMany(a => a.Books)
                .WithMany(b => b.Authors)
                .UsingEntity<Dictionary<string, object>>(
                    "AuthorBook",
                    ab => ab.HasOne<Book>().WithMany().HasForeignKey("BookId"),
                    ab => ab.HasOne<Author>().WithMany().HasForeignKey("AuthorId")
                )
                .ToTable("AuthorBook", c => c.IsTemporal());

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var foreignKey in entityType.GetForeignKeys())
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }

            modelBuilder.Entity<Author>().HasData(
                new Author { Id = 10, FirstName = "Erich",      LastName = "Gamma" },
                new Author { Id = 11, FirstName = "Richard",    LastName = "Helm" },
                new Author { Id = 12, FirstName = "Ralph",      LastName = "Johnson" },
                new Author { Id = 13, FirstName = "John",       LastName = "Vlissides" }
                );


        }

    }
}