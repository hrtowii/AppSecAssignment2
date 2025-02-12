using Assignment2.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment2
{
    public class MyDbContext(IConfiguration configuration) : DbContext
    {
        private readonly IConfiguration _configuration = configuration;

        // The options are injected via DI.
        // public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        // {
        // }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string? connectionString = _configuration.GetConnectionString("MyConnection");
            if (connectionString != null)
            {
                optionsBuilder.UseMySQL(connectionString);
            }
        }

        // DbSet properties for each model.
        public DbSet<User> Users { get; set; }
        // If you add other models later (e.g., Registration entries, etc.), add here.
        // For example, if you had separate entities for registrations, add them. 
        // In our case, our Registration uses the RegistrationViewModel for input then creates a User.
        
        // You can add other DbSets as needed.
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Optionally configure relationships here.
            // For example, if you want to set an index on Email:
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}