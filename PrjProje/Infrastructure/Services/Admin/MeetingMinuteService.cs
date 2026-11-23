using System.Globalization;
using System.Linq.Expressions;
using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PrjProje.Infrastructure.Services.Admin;

public sealed class MeetingMinuteService : IMeetingMinuteService
{
    private const int PageSize = 20;
    private static readonly StringComparer Turkish = StringComparer.Create(new CultureInfo("tr-TR"), ignoreCase: true);

    private readonly ApplicationDbContext _db;
    private readonly IActivityLogger _activityLogger;

    public MeetingMinuteService(ApplicationDbContext db, IActivityLogger activityLogger)
    {
        _db = db;
        _activityLogger = activityLogger;
    }

    /// <summary>The one access rule, applied in the database query of every method.</summary>
    public static Expression<Func<MeetingMinute, bool>> VisibleTo(string userId) =>
        m => m.PreparedById == userId || m.ParticipantUsers.Any(p => p.UserId == userId);

    private IQueryable<MeetingMinute> Visible(string userId) => _db.MeetingMinutes.Where(VisibleTo(userId));

    public async Task<PagedResult<MeetingMinuteListItem>> GetListAsync(string userId, string? search, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = Visible(userId).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m => m.Number.Contains(term) || m.Title.Contains(term) || m.Decisions.Contains(term)
                || (m.Participants != null && m.Participants.Contains(term))
                || m.ParticipantUsers.Any(p => p.FullName.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.MeetingDate).ThenByDescending(m => m.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new MeetingMinuteListItem(m.Id, m.Number, m.Title, m.MeetingDate, m.Location, m.PreparedBy))
            .ToListAsync(cancellationToken);

        return new PagedResult<MeetingMinuteListItem>(items, total, page, PageSize);
    }

    public async Task<MeetingMinuteDto?> GetAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var m = await Visible(userId).AsNoTracking().Include(x => x.ParticipantUsers).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return m is null ? null : new MeetingMinuteDto(m.Id, m.Number, m.Title, m.MeetingDate, m.Location,
            m.ParticipantUsers.Select(p => p.FullName).Order(Turkish).ToList(),
            m.Participants, m.Agenda, m.Decisions, m.Notes, m.PreparedBy, m.CreatedDate, m.UpdatedDate);
    }

    public async Task<MeetingMinuteEditModel?> GetForEditAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var m = await Visible(userId).AsNoTracking().Include(x => x.ParticipantUsers).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return m is null ? null : new MeetingMinuteEditModel
        {
            Id = m.Id,
            Number = m.Number,
            Title = m.Title,
            MeetingDate = m.MeetingDate,
            Location = m.Location,
            ParticipantUserIds = m.ParticipantUsers.Select(p => p.UserId).ToList(),
            Participants = m.Participants,
            Agenda = m.Agenda,
            Decisions = m.Decisions,
            Notes = m.Notes
        };
    }

    public async Task<IReadOnlyList<UserOption>> GetUserOptionsAsync(IEnumerable<string> selectedIds, CancellationToken cancellationToken = default)
    {
        var selected = selectedIds.ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => (u.IsActive && u.FullName != "") || selected.Contains(u.Id))
            .Select(u => new UserOption(u.Id, u.FullName == "" ? u.Email ?? u.Id : u.FullName, u.Email))
            .ToListAsync(cancellationToken);
        return users.OrderBy(u => u.FullName, Turkish).ToList();
    }

    public async Task<IReadOnlyList<string>> GetParticipantSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        // The "Name – Department" spellings people already used for participants without a panel account.
        var recent = await _db.MeetingMinutes.AsNoTracking()
            .Where(m => m.Participants != null)
            .OrderByDescending(m => m.Id)
            .Take(50)
            .Select(m => m.Participants)
            .ToListAsync(cancellationToken);

        return recent.SelectMany(p => MeetingMinuteDto.Lines(p)).Distinct(Turkish).Order(Turkish).ToList();
    }

    public async Task<ServiceResult<int>> SaveAsync(MeetingMinuteEditModel model, string userId, string userName, CancellationToken cancellationToken = default)
    {
        MeetingMinute? minute;
        if (model.Id == 0)
        {
            minute = new MeetingMinute { PreparedBy = userName, PreparedById = userId };
            _db.MeetingMinutes.Add(minute);
        }
        else
        {
            minute = await Visible(userId).Include(m => m.ParticipantUsers).FirstOrDefaultAsync(m => m.Id == model.Id, cancellationToken);
            if (minute is null)
            {
                return ServiceResult<int>.Failure("Tutanak bulunamadı.");
            }

            minute.UpdatedDate = DateTime.UtcNow;
        }

        minute.Title = model.Title.Trim();
        // Entered as the local wall-clock time of the meeting and printed exactly as typed.
        minute.MeetingDate = model.MeetingDate!.Value;
        minute.Location = AdminHelpers.Clean(model.Location);
        minute.Participants = AdminHelpers.Clean(model.Participants);
        minute.Agenda = AdminHelpers.Clean(model.Agenda);
        minute.Decisions = model.Decisions.Trim();
        minute.Notes = AdminHelpers.Clean(model.Notes);
        // A participant editing the minutes cannot untick themselves out of them; the writer always keeps access anyway.
        var participantIds = minute.PreparedById == userId ? model.ParticipantUserIds : model.ParticipantUserIds.Append(userId);
        await SetParticipantsAsync(minute, participantIds, cancellationToken);

        if (model.Id != 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            await _activityLogger.LogAsync("Updated", "MeetingMinute", minute.Id, minute.Number, cancellationToken);
            return ServiceResult<int>.Success(minute.Id);
        }

        // The unique (Year, Sequence) index settles a race between two people saving at once: the loser takes the next number.
        minute.Year = DateTime.Now.Year;
        for (var attempt = 1; ; attempt++)
        {
            minute.Sequence = await _db.MeetingMinutes.Where(m => m.Year == minute.Year).MaxAsync(m => (int?)m.Sequence, cancellationToken) + 1 ?? 1;
            minute.Number = MeetingMinute.FormatNumber(minute.Year, minute.Sequence);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException) when (attempt < 3)
            {
            }
        }

        await _activityLogger.LogAsync("Created", "MeetingMinute", minute.Id, minute.Number, cancellationToken);
        return ServiceResult<int>.Success(minute.Id);
    }

    /// <summary>Keeps the ticked users that exist; a participant who stays keeps the name copied when first added.</summary>
    private async Task SetParticipantsAsync(MeetingMinute minute, IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var wanted = userIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        foreach (var gone in minute.ParticipantUsers.Where(p => !wanted.Contains(p.UserId)).ToList())
        {
            minute.ParticipantUsers.Remove(gone);
        }

        var newIds = wanted.Except(minute.ParticipantUsers.Select(p => p.UserId)).ToList();
        var newUsers = await _db.Users.AsNoTracking()
            .Where(u => newIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(cancellationToken);
        foreach (var user in newUsers)
        {
            minute.ParticipantUsers.Add(new MeetingMinuteParticipant { UserId = user.Id, FullName = user.FullName == "" ? user.Email ?? user.Id : user.FullName });
        }
    }

    public async Task<ServiceResult> DeleteAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var minute = await Visible(userId).FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (minute is null)
        {
            return ServiceResult.Failure("Tutanak bulunamadı.");
        }

        _db.MeetingMinutes.Remove(minute);
        await _db.SaveChangesAsync(cancellationToken);
        await _activityLogger.LogAsync("Deleted", "MeetingMinute", id, minute.Number, cancellationToken);
        return ServiceResult.Success();
    }
}
