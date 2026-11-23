using PrjProje.Application.Interfaces;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace PrjProje.Infrastructure.Services.Support;

public sealed class ActivityLogger : IActivityLogger
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActivityLogger(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, object? entityId, string? description, CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userName = user?.Identity?.IsAuthenticated == true ? user.Identity.Name ?? "-" : "Website";

        _db.ActivityLogs.Add(new ActivityLog
        {
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId?.ToString(),
            Description = description is { Length: > 500 } ? description[..500] : description
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
