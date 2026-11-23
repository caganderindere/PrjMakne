using PrjProje.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PrjProje.Infrastructure.Data.Configurations;

public class MeetingMinuteConfiguration : IEntityTypeConfiguration<MeetingMinute>
{
    public void Configure(EntityTypeBuilder<MeetingMinute> builder)
    {
        builder.Property(x => x.Number).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.Participants).HasMaxLength(2000);
        builder.Property(x => x.PreparedBy).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PreparedById).HasMaxLength(450);
        // Two people saving at the same moment cannot get the same number: the second save retries with the next one.
        builder.HasIndex(x => new { x.Year, x.Sequence }).IsUnique();
        builder.HasIndex(x => x.MeetingDate);
        builder.HasIndex(x => x.PreparedById);
        builder.HasMany(x => x.ParticipantUsers).WithOne().HasForeignKey(p => p.MeetingMinuteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class MeetingMinuteParticipantConfiguration : IEntityTypeConfiguration<MeetingMinuteParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingMinuteParticipant> builder)
    {
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.MeetingMinuteId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}

public class ReferenceConfiguration : IEntityTypeConfiguration<Reference>
{
    public void Configure(EntityTypeBuilder<Reference> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Logo).HasMaxLength(300).IsRequired();
        builder.Property(x => x.WebsiteUrl).HasMaxLength(300);
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

public class BusinessCardConfiguration : IEntityTypeConfiguration<BusinessCard>
{
    public void Configure(EntityTypeBuilder<BusinessCard> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(60).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.Property(x => x.TitleTr).HasMaxLength(100);
        builder.Property(x => x.TitleEn).HasMaxLength(100);
        builder.Property(x => x.DepartmentTr).HasMaxLength(100);
        builder.Property(x => x.DepartmentEn).HasMaxLength(100);
        builder.Property(x => x.NoteTr).HasMaxLength(400);
        builder.Property(x => x.NoteEn).HasMaxLength(400);
        builder.Property(x => x.Mobile).HasMaxLength(30);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.WhatsApp).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(120);
        builder.Property(x => x.LinkedIn).HasMaxLength(200);
        builder.Property(x => x.Instagram).HasMaxLength(200);
        builder.Property(x => x.Website).HasMaxLength(200);
        builder.Property(x => x.Photo).HasMaxLength(300);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public class FairConfiguration : IEntityTypeConfiguration<Fair>
{
    public void Configure(EntityTypeBuilder<Fair> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.Property(x => x.TitleTr).HasMaxLength(150);
        builder.Property(x => x.TitleEn).HasMaxLength(150);
        builder.Property(x => x.IntroTr).HasMaxLength(400);
        builder.Property(x => x.IntroEn).HasMaxLength(400);
        builder.Property(x => x.BrochurePdf).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public class FairLeadConfiguration : IEntityTypeConfiguration<FairLead>
{
    public void Configure(EntityTypeBuilder<FairLead> builder)
    {
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.Property(x => x.LanguageCode).HasMaxLength(5).IsRequired();
        builder.HasIndex(x => new { x.FairId, x.Email }).IsUnique();
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
