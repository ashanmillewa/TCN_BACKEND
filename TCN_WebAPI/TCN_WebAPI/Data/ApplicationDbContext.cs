using Microsoft.EntityFrameworkCore;
using MySqlX.XDevAPI;
using TCN_WebAPI.Models;

namespace TCN_WebAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<AudioFile> AudioFiles { get; set; }

    }
}

