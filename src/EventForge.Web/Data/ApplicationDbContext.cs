using EventForge.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<EventVendor> EventVendors => Set<EventVendor>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EventVendor>()
            .HasKey(ev => new { ev.EventId, ev.VendorId });

        builder.Entity<Customer>().HasIndex(c => c.Email).IsUnique();
        builder.Entity<Customer>().HasIndex(c => c.Phone).IsUnique();
        builder.Entity<Lead>().HasIndex(l => l.Status);
        builder.Entity<Lead>().HasIndex(l => l.AssignedTo);
        builder.Entity<Opportunity>().HasIndex(o => o.Stage);
        builder.Entity<Opportunity>().HasIndex(o => o.AssignedTo);
        builder.Entity<Opportunity>().HasIndex(o => o.ExpectedCloseDate);
        builder.Entity<FollowUp>().HasIndex(f => f.FollowUpDate);
        builder.Entity<Event>().HasIndex(e => e.EventDate);
        builder.Entity<Event>().HasIndex(e => e.Status);
    }
}