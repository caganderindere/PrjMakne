using PrjProje.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PrjProje.Infrastructure.Data.Configurations;

public class ReferenceConfiguration : IEntityTypeConfiguration<Reference>
{
    public void Configure(EntityTypeBuilder<Reference> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Logo).HasMaxLength(300).IsRequired();
        builder.Property(x => x.WebsiteUrl).HasMaxLength(300);
    }
}

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.Property(x => x.YouTubeId).HasMaxLength(11).IsRequired();
        builder.Property(x => x.TitleTr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.TitleEn).HasMaxLength(150);
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

public class LegacyRedirectConfiguration : IEntityTypeConfiguration<LegacyRedirect>
{
    public void Configure(EntityTypeBuilder<LegacyRedirect> builder)
    {
        builder.Property(x => x.FromPath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.ToPath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.ToPathEn).HasMaxLength(400);
        builder.HasIndex(x => x.FromPath).IsUnique();
    }
}

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.Property(x => x.MessageTr).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MessageEn).HasMaxLength(500).IsRequired();
        builder.Property(x => x.LinkUrl).HasMaxLength(300);
        builder.Property(x => x.LinkTextTr).HasMaxLength(60);
        builder.Property(x => x.LinkTextEn).HasMaxLength(60);
        builder.Property(x => x.Image).HasMaxLength(300);
        builder.HasIndex(x => new { x.IsActive, x.StartDate });
    }
}

public class ContentTextConfiguration : IEntityTypeConfiguration<ContentText>
{
    public void Configure(EntityTypeBuilder<ContentText> builder)
    {
        builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Value).IsRequired();
        builder.HasIndex(x => new { x.Key, x.LanguageCode }).IsUnique();
    }
}
