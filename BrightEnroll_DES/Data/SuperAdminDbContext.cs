using Microsoft.EntityFrameworkCore;
using BrightEnroll_DES.Data.Models;
using BrightEnroll_DES.Data.Models.SuperAdmin;

namespace BrightEnroll_DES.Data;

// EF Core database context for SuperAdmin database - handles SuperAdmin management tables only
public class SuperAdminDbContext : DbContext
{
    // SuperAdmin users table (for local development/testing - production uses cloud)
    public DbSet<UserEntity> Users { get; set; }
    
    // SuperAdmin management tables
    public DbSet<Customer> Customers { get; set; }
    // SalesLeads removed - Sales Lead functionality not needed
    public DbSet<SupportTicket> SupportTickets { get; set; }
    // Contracts removed - Contract information is stored in tbl_Customers table
    // (ContractStartDate, ContractEndDate, ContractDurationMonths, ContractTermsText, etc.)
    public DbSet<SystemUpdate> SystemUpdates { get; set; }
    
    // SuperAdmin BIR Information
    public DbSet<SuperAdminBIRInfo> SuperAdminBIRInfo { get; set; }
    
    // SuperAdmin BIR Filings/Submissions
    public DbSet<SuperAdminBIRFiling> SuperAdminBIRFilings { get; set; }

    // Accounts Receivable
    public DbSet<CustomerInvoice> CustomerInvoices { get; set; }
    public DbSet<CustomerPayment> CustomerPayments { get; set; }

    public SuperAdminDbContext(DbContextOptions<SuperAdminDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserEntity for SuperAdmin database
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.ToTable("tbl_Users");
            entity.HasIndex(e => e.SystemId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId);
            entity.ToTable("tbl_Customers");
            entity.HasIndex(e => e.CustomerCode).IsUnique();
            entity.HasIndex(e => e.SchoolName);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ContractEndDate);
            
            // CreatedBy column exists but references tbl_Users in main database, no FK constraint here
        });

        // SalesLead entity removed - Sales Lead functionality not needed

        // Configure SupportTicket entity
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId);
            entity.ToTable("tbl_SupportTickets");
            entity.HasIndex(e => e.TicketNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasOne(t => t.Customer)
                  .WithMany()
                  .HasForeignKey(t => t.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // AssignedTo column exists but references tbl_Users in main database, no FK constraint here
        });

        // Contracts entity removed - Contract information is stored in tbl_Customers table
        // (ContractStartDate, ContractEndDate, ContractDurationMonths, ContractTermsText, etc.)

        // Configure SystemUpdate entity
        modelBuilder.Entity<SystemUpdate>(entity =>
        {
            entity.HasKey(e => e.UpdateId);
            entity.ToTable("tbl_SystemUpdates");
            entity.HasIndex(e => e.VersionNumber).IsUnique();
            entity.HasIndex(e => e.ReleaseDate);
            entity.HasIndex(e => e.Status);
            
            // CreatedBy column exists but references tbl_Users in main database, no FK constraint here
        });

        // Configure SuperAdminBIRInfo entity
        modelBuilder.Entity<SuperAdminBIRInfo>(entity =>
        {
            entity.HasKey(e => e.BirInfoId);
            entity.ToTable("tbl_SuperAdminBIRInfo");
        });

        // Configure SuperAdminBIRFiling entity
        modelBuilder.Entity<SuperAdminBIRFiling>(entity =>
        {
            entity.HasKey(e => e.FilingId);
            entity.ToTable("tbl_SuperAdminBIRFilings");
            entity.HasIndex(e => e.FilingType);
            entity.HasIndex(e => e.Period);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.FilingDate);
        });

        // Configure CustomerInvoice entity
        modelBuilder.Entity<CustomerInvoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId);
            entity.ToTable("tbl_CustomerInvoices");
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.InvoiceDate);
            
            // Ignore UpdatedAt property since the column doesn't exist in the database table
            entity.Ignore(e => e.UpdatedAt);
            
            entity.HasOne(i => i.Customer)
                  .WithMany()
                  .HasForeignKey(i => i.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure CustomerPayment entity
        modelBuilder.Entity<CustomerPayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            entity.ToTable("tbl_CustomerPayments");
            entity.HasIndex(e => e.PaymentReference);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.PaymentDate);
            
            entity.HasOne(p => p.Invoice)
                  .WithMany()
                  .HasForeignKey(p => p.InvoiceId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(p => p.Customer)
                  .WithMany()
                  .HasForeignKey(p => p.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

