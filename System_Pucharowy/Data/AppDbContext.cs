using Microsoft.EntityFrameworkCore;
using System_Pucharowy.Models;

namespace System_Pucharowy.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Tournament> Tournaments => Set<Tournament>();
        public DbSet<Bracket> Brackets => Set<Bracket>();
        public DbSet<Match> Matches => Set<Match>();
    }
}
