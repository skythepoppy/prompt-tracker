using Microsoft.EntityFrameworkCore;
using PromptTrackerv1.Models;

namespace PromptTrackerAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Prompt> Prompts { get; set; }
    }
}
