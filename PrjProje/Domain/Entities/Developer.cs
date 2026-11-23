 using PrjProje.Domain.Common;
using PrjProje.Domain.Enums;

namespace PrjProje.Domain.Entities;

/// <summary>A bug report or feature request sent to the developer from the admin panel.</summary>
public class DeveloperTicket : BaseEntity
{
    public TicketType Type { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DeveloperNote { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentOriginalName { get; set; }
    public long? AttachmentSize { get; set; }

    /// <summary>Plain values rather than a foreign key, so a report outlives the account that sent it.</summary>
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
}

/// <summary>Request totals of one route during one hour. Holds no visitor or IP data.</summary>
public class RequestMetricHour
{
    public int Id { get; set; }
    public DateTime HourUtc { get; set; }
    public string Route { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int ClientErrorCount { get; set; }
    public int ServerErrorCount { get; set; }
    public long TotalMs { get; set; }
    public int Under100Ms { get; set; }
    public int Under250Ms { get; set; }
    public int Under500Ms { get; set; }
    public int Under1000Ms { get; set; }
    public int Under2500Ms { get; set; }
    public int Over2500Ms { get; set; }
}

public class ServerMetricHour
{
    public DateTime HourUtc { get; set; }
    public long PeakMemoryBytes { get; set; }
}
