using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class VideoAdminService : IVideoAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _activityLogger;
    private readonly IContentCache _cache;

    public VideoAdminService(ApplicationDbContext db, IActivityLogger activityLogger, IContentCache cache)
    {
        _db = db;
        _activityLogger = activityLogger;
        _cache = cache;
    }

    public async Task<IReadOnlyList<VideoAdminItem>> GetListAsync(CancellationToken cancellationToken = default) =>
        await _db.Videos.AsNoTracking()
            .OrderBy(v => v.SortOrder).ThenByDescending(v => v.Id)
            .Select(v => new VideoAdminItem(v.Id, v.YouTubeId, v.TitleTr, v.TitleEn, v.SortOrder, v.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<VideoEditModel?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var video = await _db.Videos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return video is null ? null : new VideoEditModel
        {
            Id = video.Id,
            Url = $"https://www.youtube.com/watch?v={video.YouTubeId}",
            TitleTr = video.TitleTr,
            TitleEn = video.TitleEn,
            SortOrder = video.SortOrder,
            IsActive = video.IsActive
        };
    }

    public async Task<ServiceResult<int>> SaveAsync(VideoEditModel model, CancellationToken cancellationToken = default)
    {
        if (YouTubeId.Parse(model.Url) is not { } youTubeId)
        {
            return ServiceResult<int>.Failure("Geçerli bir YouTube video bağlantısı girin (ör. https://www.youtube.com/watch?v=...).");
        }

        Video? video;
        var isNew = model.Id == 0;
        if (isNew)
        {
            video = new Video();
            _db.Videos.Add(video);
        }
        else
        {
            video = await _db.Videos.FirstOrDefaultAsync(v => v.Id == model.Id, cancellationToken);
            if (video is null)
            {
                return ServiceResult<int>.Failure("Video bulunamadı.");
            }

            video.UpdatedDate = DateTime.UtcNow;
        }

        video.YouTubeId = youTubeId;
        video.TitleTr = model.TitleTr.Trim();
        video.TitleEn = AdminHelpers.Clean(model.TitleEn);
        video.SortOrder = model.SortOrder;
        video.IsActive = model.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync(isNew ? "Created" : "Updated", "Video", video.Id, video.TitleTr, cancellationToken);
        return ServiceResult<int>.Success(video.Id);
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var video = await _db.Videos.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (video is null)
        {
            return ServiceResult.Failure("Video bulunamadı.");
        }

        _db.Videos.Remove(video);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Invalidate();
        await _activityLogger.LogAsync("Deleted", "Video", id, video.TitleTr, cancellationToken);
        return ServiceResult.Success();
    }
}
