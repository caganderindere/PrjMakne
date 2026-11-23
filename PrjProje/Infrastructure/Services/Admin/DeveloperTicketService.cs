using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Domain.Enums;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class DeveloperTicketService : IDeveloperTicketService
{
    private const int PageSize = 20;
    private static readonly string[] ScreenshotExtensions = { ".png", ".jpg", ".jpeg" };

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly IActivityLogger _activityLogger;

    public DeveloperTicketService(ApplicationDbContext db, IFileStorageService storage, IActivityLogger activityLogger)
    {
        _db = db;
        _storage = storage;
        _activityLogger = activityLogger;
    }

    public async Task<PagedResult<TicketListItem>> GetListAsync(TicketViewer viewer, TicketStatus? status, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = Visible(viewer);
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(t => new TicketListItem(t.Id, t.Type, t.Priority, t.Status, t.Title, t.CreatedByName, t.AttachmentPath != null, t.CreatedDate))
            .ToListAsync(cancellationToken);

        return new PagedResult<TicketListItem>(items, total, page, PageSize);
    }

    public Task<TicketDetail?> GetAsync(TicketViewer viewer, int id, CancellationToken cancellationToken = default) =>
        Visible(viewer)
            .Where(t => t.Id == id)
            .Select(t => new TicketDetail(t.Id, t.Type, t.Priority, t.Status, t.Title, t.Description,
                viewer.IsAdmin ? t.DeveloperNote : null,
                t.AttachmentOriginalName, t.AttachmentSize, t.CreatedByName, t.CreatedDate, t.UpdatedDate))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateAsync(TicketViewer viewer, string userName, TicketCreateModel model, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(model.Type) || !Enum.IsDefined(model.Priority))
        {
            return ServiceResult.Failure("Geçersiz tür veya önem.");
        }

        var ticket = new DeveloperTicket
        {
            Type = model.Type,
            Priority = model.Priority,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            CreatedById = viewer.UserId,
            CreatedByName = userName
        };

        if (model.Screenshot is not null)
        {
            var extension = Path.GetExtension(model.Screenshot.FileName);
            var stored = ScreenshotExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                ? await _storage.SavePrivateAttachmentAsync(model.Screenshot, cancellationToken)
                : null;
            if (stored is not { Succeeded: true, Value: not null })
            {
                return ServiceResult.Failure("Ekran görüntüsü PNG veya JPG biçiminde ve en fazla 10 MB olmalıdır.");
            }

            ticket.AttachmentPath = stored.Value.RelativePath;
            ticket.AttachmentOriginalName = stored.Value.OriginalName;
            ticket.AttachmentSize = stored.Value.Size;
        }

        _db.DeveloperTickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("Created", "DeveloperTicket", ticket.Id, ticket.Title, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAsync(TicketUpdateModel model, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.DeveloperTickets.FirstOrDefaultAsync(t => t.Id == model.Id, cancellationToken);
        if (ticket is null)
        {
            return ServiceResult.Failure("Bildirim bulunamadı.");
        }

        if (!Enum.IsDefined(model.Status))
        {
            return ServiceResult.Failure("Geçersiz durum.");
        }

        var previous = ticket.Status;
        ticket.Status = model.Status;
        ticket.DeveloperNote = AdminHelpers.Clean(model.DeveloperNote);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("StatusChanged", "DeveloperTicket", ticket.Id, $"{previous} → {model.Status}", cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<QuoteAttachment?> GetAttachmentAsync(TicketViewer viewer, int id, CancellationToken cancellationToken = default)
    {
        var ticket = await Visible(viewer)
            .Where(t => t.Id == id && t.AttachmentPath != null)
            .Select(t => new { t.AttachmentPath, t.AttachmentOriginalName })
            .FirstOrDefaultAsync(cancellationToken);

        var physicalPath = ticket?.AttachmentPath is null ? null : _storage.GetPrivateAttachmentPhysicalPath(ticket.AttachmentPath);
        return physicalPath is null ? null : new QuoteAttachment(physicalPath, ticket!.AttachmentOriginalName ?? Path.GetFileName(physicalPath));
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.DeveloperTickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null)
        {
            return ServiceResult.Failure("Bildirim bulunamadı.");
        }

        _db.DeveloperTickets.Remove(ticket);
        await _db.SaveChangesAsync(cancellationToken);
        _storage.DeletePrivateFile(ticket.AttachmentPath);
        await _activityLogger.LogAsync("Deleted", "DeveloperTicket", id, ticket.Title, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<int> CountNewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.DeveloperTickets.CountAsync(t => t.Status == TicketStatus.New, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return 0;
        }
    }

    private IQueryable<DeveloperTicket> Visible(TicketViewer viewer) =>
        _db.DeveloperTickets.AsNoTracking().Where(t => viewer.IsAdmin || t.CreatedById == viewer.UserId);
}
