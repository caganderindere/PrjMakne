namespace PrjProje.Web.ViewModels;

/// <param name="Image">Page-specific photo (category, capability, blog cover); otherwise the admin banner for the page is used.</param>
public record PageHeaderModel(string Title, string? Lead = null, string? Eyebrow = null, string? Image = null);

/// <param name="Id">Unique per page: the input and its suggestion list derive their ids from it.</param>
public record ProductSearchFormModel(string Action, string Id, string? Value = null, string? CategorySlug = null);

public record PaginationModel(int Page, int TotalPages, string BasePath, IReadOnlyDictionary<string, string?> Query);
