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
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceTranslation> ServiceTranslations => Set<ServiceTranslation>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
    public DbSet<WhatsAppClickDay> WhatsAppClickDays => Set<WhatsAppClickDay>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Reference> References => Set<Reference>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<ContentText> ContentTexts => Set<ContentText>();

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
