using PrjProje.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PrjProje.Infrastructure.Data.Configurations;

public class BlogCategoryTranslationConfiguration : IEntityTypeConfiguration<BlogCategoryTranslation>
{
    public void Configure(EntityTypeBuilder<BlogCategoryTranslation> builder)
    {
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.BlogCategoryId, x.LanguageCode }).IsUnique();
        builder.HasOne(x => x.BlogCategory)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.BlogCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
{
    public void Configure(EntityTypeBuilder<BlogPost> builder)
    {
        builder.Property(x => x.CoverImage).HasMaxLength(300);
        builder.Property(x => x.AuthorName).HasMaxLength(150).IsRequired();
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class BlogPostTranslationConfiguration : IEntityTypeConfiguration<BlogPostTranslation>
{
    public void Configure(EntityTypeBuilder<BlogPostTranslation> builder)
    {
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(270).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(600);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(70);
        builder.Property(x => x.MetaDescription).HasMaxLength(170);
        builder.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.BlogPostId, x.LanguageCode }).IsUnique();
        builder.HasIndex(x => new { x.LanguageCode, x.Status, x.PublishedDate });
        builder.HasOne(x => x.BlogPost)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.BlogPostId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Tags)
            .WithMany(x => x.Posts)
            .UsingEntity(j => j.ToTable("BlogPostTags"));
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
    }
}

public class GalleryCategoryTranslationConfiguration : IEntityTypeConfiguration<GalleryCategoryTranslation>
{
    public void Configure(EntityTypeBuilder<GalleryCategoryTranslation> builder)
    {
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.GalleryCategoryId, x.LanguageCode }).IsUnique();
        builder.HasOne(x => x.GalleryCategory)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.GalleryCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GalleryItemConfiguration : IEntityTypeConfiguration<GalleryItem>
{
    public void Configure(EntityTypeBuilder<GalleryItem> builder)
    {
        builder.ToTable("Galleries");
        builder.Property(x => x.ImagePath).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => new { x.IsActive, x.CategoryId, x.SortOrder });
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GalleryItemTranslationConfiguration : IEntityTypeConfiguration<GalleryItemTranslation>
{
    public void Configure(EntityTypeBuilder<GalleryItemTranslation> builder)
    {
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.GalleryItemId, x.LanguageCode }).IsUnique();
        builder.HasOne(x => x.GalleryItem)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.GalleryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CapabilityConfiguration : IEntityTypeConfiguration<Capability>
{
    public void Configure(EntityTypeBuilder<Capability> builder)
    {
        builder.Property(x => x.IconClass).HasMaxLength(100);
        builder.Property(x => x.Image).HasMaxLength(300);
        builder.HasOne(x => x.GalleryCategory)
            .WithMany()
            .HasForeignKey(x => x.GalleryCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(x => x.ProductCategories)
            .WithMany()
            .UsingEntity(j => j.ToTable("CapabilityProductCategories"));
    }
}

public class CapabilityTranslationConfiguration : IEntityTypeConfiguration<CapabilityTranslation>
{
    public void Configure(EntityTypeBuilder<CapabilityTranslation> builder)
    {
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.Property(x => x.Specifications).HasMaxLength(3000);
        builder.Property(x => x.MetaTitle).HasMaxLength(70);
        builder.Property(x => x.MetaDescription).HasMaxLength(170);
        builder.HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.CapabilityId, x.LanguageCode }).IsUnique();
        builder.HasOne(x => x.Capability)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.CapabilityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.FilePath).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CoverImagePath).HasMaxLength(300);
        builder.Property(x => x.LanguageCode).HasMaxLength(5);
    }
}

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Company).HasMaxLength(150);
        builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(5);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.HasIndex(x => new { x.IsRead, x.CreatedDate });
    }
}

public class WhatsAppClickDayConfiguration : IEntityTypeConfiguration<WhatsAppClickDay>
{
    public void Configure(EntityTypeBuilder<WhatsAppClickDay> builder) => builder.HasKey(x => x.Day);
}

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.City).HasMaxLength(80);
        builder.Property(x => x.Position).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(4000);
        builder.Property(x => x.ResumePath).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ResumeOriginalName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(5);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.HasIndex(x => new { x.IsRead, x.CreatedDate });
    }
}

public class QuoteRequestConfiguration : IEntityTypeConfiguration<QuoteRequest>
{
    public void Configure(EntityTypeBuilder<QuoteRequest> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CompanyName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(200);
        builder.Property(x => x.Quantity).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.AttachmentPath).HasMaxLength(300);
        builder.Property(x => x.AttachmentOriginalName).HasMaxLength(255);
        builder.Property(x => x.AdminNote).HasMaxLength(2000);
        builder.Property(x => x.LanguageCode).HasMaxLength(5);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.HasIndex(x => new { x.Status, x.CreatedDate });
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.Property(x => x.Key).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.Property(x => x.UserName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.CreatedDate);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
    }
}
