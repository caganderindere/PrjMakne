using System.ComponentModel.DataAnnotations;
using PrjProje.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace PrjProje.Application.Models.Admin;

/// <summary>Who is asking: admins see every ticket and its notes, others only their own tickets.</summary>
public record TicketViewer(string UserId, bool IsAdmin);

public record TicketListItem(
    int Id,
    TicketType Type,
    TicketPriority Priority,
    TicketStatus Status,
    string Title,
    string CreatedByName,
    bool HasAttachment,
    DateTime CreatedDate);

public record TicketDetail(
    int Id,
    TicketType Type,
    TicketPriority Priority,
    TicketStatus Status,
    string Title,
    string Description,
    string? DeveloperNote,
    string? AttachmentOriginalName,
    long? AttachmentSize,
    string CreatedByName,
    DateTime CreatedDate,
    DateTime? UpdatedDate);

public class TicketCreateModel
{
    [Display(Name = "Tür")]
    public TicketType Type { get; set; } = TicketType.Bug;

    [Display(Name = "Önem")]
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    [Display(Name = "Başlık")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(4000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Ekran Görüntüsü")]
    public IFormFile? Screenshot { get; set; }
}

public class TicketUpdateModel
{
    public int Id { get; set; }

    [Display(Name = "Durum")]
    public TicketStatus Status { get; set; }

    [Display(Name = "Geliştirici Notu")]
    [StringLength(2000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? DeveloperNote { get; set; }
}

public record PerformancePoint(
    string Label,
    long Requests,
    double? AverageMs,
    int? P95Ms,
    double? ClientErrorPercent,
    double? ServerErrorPercent,
    double? MemoryMb);

public record PerformanceRoute(string Route, long Requests, double AverageMs, int? P95Ms, double ErrorPercent);

public class PerformanceReport
{
    public IReadOnlyList<PerformancePoint> Points { get; init; } = Array.Empty<PerformancePoint>();
    public IReadOnlyList<PerformanceRoute> SlowestRoutes { get; init; } = Array.Empty<PerformanceRoute>();
    public long TotalRequests { get; init; }
    public double? AverageMs { get; init; }
    public int? P95Ms { get; init; }
    public double? ServerErrorPercent { get; init; }
    public double MemoryMb { get; init; }
    public string Uptime { get; init; } = string.Empty;
}
