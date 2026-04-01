
using Microsoft.EntityFrameworkCore;
using GmailClone.Models;

namespace GmailClone.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<EmailMessage> Emails { get; set; }
}
