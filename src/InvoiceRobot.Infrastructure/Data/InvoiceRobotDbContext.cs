using InvoiceRobot.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceRobot.Infrastructure.Data;

public class InvoiceRobotDbContext : DbContext
{
    public InvoiceRobotDbContext(DbContextOptions<InvoiceRobotDbContext> options)
        : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NetvisorInvoiceKey).IsUnique();
            entity.HasIndex(e => e.InvoiceNumber);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.VendorName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.SuggestedProject)
                .WithMany(p => p.SuggestedInvoices)
                .HasForeignKey(e => e.SuggestedProjectKey)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FinalProject)
                .WithMany(p => p.FinalInvoices)
                .HasForeignKey(e => e.FinalProjectKey)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NetvisorProjectKey).IsUnique();
            entity.HasIndex(e => e.ProjectCode).IsUnique();

            entity.Property(e => e.ProjectCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.ProjectManagerEmail).HasMaxLength(100);
        });

        // ApprovalRequest configuration
        modelBuilder.Entity<ApprovalRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Token).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(500);

            entity.HasOne(e => e.Invoice)
                .WithOne(i => i.ApprovalRequest)
                .HasForeignKey<ApprovalRequest>(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
