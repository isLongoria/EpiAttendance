using EpiAttendance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EpiAttendance.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
    
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                // Create an index on Date for faster queries
                entity.HasIndex(e => e.Date);
                
            }
        );
    }
   
}