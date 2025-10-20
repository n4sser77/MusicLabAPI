using Backend.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace MusiclabApi
{
    public class PostgresAppDb : DbContext
    {
        public PostgresAppDb(DbContextOptions<PostgresAppDb> op) : base(op) { }
        public PostgresAppDb() { }
        public DbSet<FileReference> Files { get; set; }
        public DbSet<MusicMetadata> MusicData { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
