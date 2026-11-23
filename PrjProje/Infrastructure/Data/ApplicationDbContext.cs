using PrjProje.Domain.Common;
using PrjProje.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductCategoryTranslation> ProductCategoryTranslations => Set<ProductCategoryTranslation>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogCategoryTranslation> BlogCategoryTranslations => Set<BlogCategoryTranslation>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogPostTranslation> BlogPostTranslations => Set<BlogPostTranslation>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<GalleryCategory> GalleryCategories => Set<GalleryCategory>();
    public DbSet<GalleryCategoryTranslation> GalleryCategoryTranslations => Set<GalleryCategoryTranslation>();
    public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
    public DbSet<GalleryItemTranslation> GalleryItemTranslations => Set<GalleryItemTranslation>();
    public DbSet<Capability> Capabilities => Set<Capability>();
    public DbSet<CapabilityTranslation> CapabilityTranslations => Set<CapabilityTranslation>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<WhatsAppClickDay> WhatsAppClickDays => Set<WhatsAppClickDay>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Reference> References => Set<Reference>();
    public DbSet<MeetingMinute> MeetingMinutes => Set<MeetingMinute>();
    public DbSet<MeetingMinuteParticipant> MeetingMinuteParticipants => Set<MeetingMinuteParticipant>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<ContentText> ContentTexts => Set<ContentText>();
    public DbSet<BusinessCard> BusinessCards => Set<BusinessCard>();
    public DbSet<Fair> Fairs => Set<Fair>();
    public DbSet<FairLead> FairLeads => Set<FairLead>();
    public DbSet<DeveloperTicket> DeveloperTickets => Set<DeveloperTicket>();
    public DbSet<RequestMetricHour> RequestMetricHours => Set<RequestMetricHour>();
    public DbSet<ServerMetricHour> ServerMetricHours => Set<ServerMetricHour>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedDate = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
