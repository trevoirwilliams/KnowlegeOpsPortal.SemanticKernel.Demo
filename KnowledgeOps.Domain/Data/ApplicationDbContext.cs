using System;
using KnowledgeOps.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Domain.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options){
    public DbSet<CopilotConversation> CopilotConversations => Set<CopilotConversation>();

    public DbSet<CopilotMessage> CopilotMessages => Set<CopilotMessage>();

    public DbSet<PortalDocument> PortalDocuments => Set<PortalDocument>();
    
    public DbSet<BusinessRequest> BusinessRequests => Set<BusinessRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CopilotConversation>(entity =>
        {
            entity.Property(e => e.ContextType)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.UserId, e.ContextType, e.ContextId });
        });

        modelBuilder.Entity<CopilotMessage>(entity =>
        {
            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.ConversationId, e.SequenceNumber });
        });

        modelBuilder.Entity<PortalDocument>(entity =>
        {
            entity.Property(e => e.ProcessingStatus)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => new { e.UserId, e.ProcessingStatus });
        });
    }
}
