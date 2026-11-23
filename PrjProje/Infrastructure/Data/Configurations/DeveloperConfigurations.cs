using PrjProje.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PrjProje.Infrastructure.Data.Configurations;

public class DeveloperTicketConfiguration : IEntityTypeConfiguration<DeveloperTicket>
{
    public void Configure(EntityTypeBuilder<DeveloperTicket> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.DeveloperNote).HasMaxLength(2000);
        builder.Property(x => x.AttachmentPath).HasMaxLength(300);
        builder.Property(x => x.AttachmentOriginalName).HasMaxLength(255);
        builder.Property(x => x.CreatedById).HasMaxLength(450).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.Status, x.CreatedDate });
        builder.HasIndex(x => x.CreatedById);
    }
}

public class RequestMetricHourConfiguration : IEntityTypeConfiguration<RequestMetricHour>
{
    public void Configure(EntityTypeBuilder<RequestMetricHour> builder)
    {
        builder.Property(x => x.Route).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.HourUtc, x.Route }).IsUnique();
    }
}

public class ServerMetricHourConfiguration : IEntityTypeConfiguration<ServerMetricHour>
{
    public void Configure(EntityTypeBuilder<ServerMetricHour> builder)
    {
        builder.HasKey(x => x.HourUtc);
    }
}
