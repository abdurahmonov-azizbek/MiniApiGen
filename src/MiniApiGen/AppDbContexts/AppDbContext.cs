using Microsoft.EntityFrameworkCore;
using MiniApiGen.Entities;

namespace MiniApiGen.AppDbContexts;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }
}
    