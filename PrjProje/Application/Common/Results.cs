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
    public const string Editor = "Editor";

    /// <summary>Human resources staff: sees only the job applications, nothing else in the panel.</summary>
    public const string HumanResources = "HumanResources";

    /// <summary>R&amp;D team: everything the admin can do except managing users and their roles.</summary>
    public const string RnD = "RnD";

    /// <summary>Marketing: analytics, quotes, contact messages, job applications, fairs and business cards.</summary>
    public const string Marketing = "Marketing";

    /// <summary>Staff who only use the modules (meeting minutes): they can be picked as participants and see nothing else.</summary>
    public const string Employee = "Employee";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Editor, HumanResources, RnD, Marketing, Employee };

    public static string Name(string role) => role switch
    {
        Admin => "Yönetici",
        HumanResources => "İnsan Kaynakları",
        RnD => "Ar-Ge Ekibi",
        Marketing => "Pazarlama",
        Employee => "Personel",
        _ => "Editör"
    };

    /// <summary>A user has one role; this picks the widest should they ever hold several. Editor is the fallback for none.</summary>
    public static string Primary(Func<string, bool> isInRole) =>
        new[] { Admin, RnD, Editor, Marketing, HumanResources, Employee }.FirstOrDefault(isInRole) ?? Editor;

    /// <summary>Admin or R&amp;D: the whole panel apart from user management.</summary>
    public static bool HasFullAccess(ClaimsPrincipal user) => user.IsInRole(Admin) || user.IsInRole(RnD);
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
    /// <summary>Admin and R&amp;D: every page except user management.</summary>
    public const string FullAccess = "FullAccess";

    /// <summary>Creating, editing and deleting panel users (and so their roles and passwords): admin only.</summary>
    public const string UserManagement = "UserManagement";

    /// <summary>Analytics, quotes, contact messages and fairs: admin, R&amp;D and marketing.</summary>
    public const string Marketing = "Marketing";

    /// <summary>Digital business cards: the content roles plus marketing.</summary>
    public const string CardManagement = "CardManagement";
    public const string ContentManagement = "ContentManagement";
    public const string HumanResources = "HumanResources";

    /// <summary>Any panel user, whatever the role (account pages).</summary>
    public const string Staff = "Staff";
}
