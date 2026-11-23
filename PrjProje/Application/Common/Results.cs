using System.Security.Claims;

namespace PrjProje.Application.Common;

public class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public class ServiceResult
{
    protected ServiceResult(bool succeeded, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public IReadOnlyList<string> Errors { get; }

    public static ServiceResult Success() => new(true, Array.Empty<string>());
    public static ServiceResult Failure(params string[] errors) => new(false, errors);
}

public class ServiceResult<T> : ServiceResult
{
    private ServiceResult(bool succeeded, T? value, IReadOnlyList<string> errors) : base(succeeded, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static ServiceResult<T> Success(T value) => new(true, value, Array.Empty<string>());
    public static new ServiceResult<T> Failure(params string[] errors) => new(false, default, errors);
}

public static class Roles
{
    public const string Admin = "Admin";

    /// <summary>Content editors: catalogue, pages, media, quotes and messages; no users or site settings.</summary>
    public const string Editor = "Editor";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Editor };

    public static string Name(string role) => role == Admin ? "Yönetici" : "Editör";

    /// <summary>A user has one role; this picks the widest should they ever hold both. Editor is the fallback for none.</summary>
    public static string Primary(Func<string, bool> isInRole) => isInRole(Admin) ? Admin : Editor;

    public static bool HasFullAccess(ClaimsPrincipal user) => user.IsInRole(Admin);
}

/// <summary>Claims the panel adds to the sign-in cookie (see PanelClaimsFactory).</summary>
public static class PanelClaims
{
    public const string FullName = "full_name";

    /// <summary>Present while the user still signs in with a password an admin set: every panel page sends them to change it.</summary>
    public const string MustChangePassword = "must_change_password";
}

public static class Policies
{
    /// <summary>Site settings and e-mail notifications: admin only.</summary>
    public const string FullAccess = "FullAccess";

    /// <summary>Creating, editing and deleting panel users (and so their roles and passwords): admin only.</summary>
    public const string UserManagement = "UserManagement";

    /// <summary>Catalogue, content, media, quotes and messages: admin and editor.</summary>
    public const string ContentManagement = "ContentManagement";

    /// <summary>Any panel user, whatever the role (account pages).</summary>
    public const string Staff = "Staff";
}
