

using Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    //// Parameterless constructor for EF CLI
    public AppDbContext() { }

    public DbSet<FileReference> Files { get; set; }
    public DbSet<MusicMetadata> MusicData { get; set; }
    public DbSet<User> Users { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{


    //    // Use your actual connection string here
    //    //optionsBuilder.UseSqlServer("Server=localhost;Database=FileUploadDEMO;Trusted_Connection=True;TrustServerCertificate=True;Integrated Security=True");

    //}
}


