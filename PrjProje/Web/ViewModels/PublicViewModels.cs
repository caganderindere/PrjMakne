using PrjProje.Application.Common;
using PrjProje.Application.Models;
using PrjProje.Infrastructure.Services.Support;

namespace PrjProje.Web.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<ProductCategoryDto> Categories { get; init; } = Array.Empty<ProductCategoryDto>();
    public IReadOnlyList<CapabilityDto> Capabilities { get; init; } = Array.Empty<CapabilityDto>();
    public IReadOnlyList<ProductListItemDto> FeaturedProducts { get; init; } = Array.Empty<ProductListItemDto>();
    public IReadOnlyList<BlogPostListItemDto> LatestPosts { get; init; } = Array.Empty<BlogPostListItemDto>();
    public IReadOnlyList<string> ExportCountryCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ReferenceDto> References { get; init; } = Array.Empty<ReferenceDto>();
    public string? HeroImage { get; init; }
    public string? HeroVideo { get; init; }
    public string? HeroVideoMobile { get; init; }

    /// <summary>Visible movable sections in display order (see SiteSettingsDto.HomeSectionKeys).</summary>
    public IReadOnlyList<string> Sections { get; init; } = Array.Empty<string>();

    public HomeStats Stats { get; init; } = new(25000, 150, 20, 100);
}

public record HomeStats(int Facility, int Employees, int Countries, int Domestic)
{
    public static HomeStats From(SiteSettingsDto settings) => new(
        SiteSettingsDto.ParseStat(settings.StatFacility, 25000),
        SiteSettingsDto.ParseStat(settings.StatEmployees, 150),
        SiteSettingsDto.ParseStat(settings.StatCountries, 20),
        SiteSettingsDto.ParseStat(settings.StatDomestic, 100));
}

public class ProductListViewModel
{
    public IReadOnlyList<ProductCategoryDto> Categories { get; init; } = Array.Empty<ProductCategoryDto>();
    public ProductCategoryDto? CurrentCategory { get; init; }
    public string? Search { get; init; }
    public PagedResult<ProductListItemDto> Products { get; init; } = new(Array.Empty<ProductListItemDto>(), 0, 1, 12);
}

public class ManufacturingViewModel
{
    public IReadOnlyList<CapabilityDto> Capabilities { get; init; } = Array.Empty<CapabilityDto>();
}

public class CapabilityDetailViewModel
{
    public CapabilityDto Capability { get; init; } = new();
    public IReadOnlyList<CapabilityDto> Others { get; init; } = Array.Empty<CapabilityDto>();
    public IReadOnlyList<GalleryItemDto> Gallery { get; init; } = Array.Empty<GalleryItemDto>();
    public IReadOnlyList<ProductCategoryDto> Categories { get; init; } = Array.Empty<ProductCategoryDto>();
    public IReadOnlyList<DocumentDto> Certificates { get; init; } = Array.Empty<DocumentDto>();
}

public class DefenseViewModel
{
    public ProductCategoryDto? Category { get; init; }
    public IReadOnlyList<ProductListItemDto> Products { get; init; } = Array.Empty<ProductListItemDto>();
}

public class BlogListViewModel
{
    public PagedResult<BlogPostListItemDto> Posts { get; init; } = new(Array.Empty<BlogPostListItemDto>(), 0, 1, 9);
    public IReadOnlyList<BlogCategoryDto> Categories { get; init; } = Array.Empty<BlogCategoryDto>();
    public string? CurrentCategorySlug { get; init; }
    public TagDto? CurrentTag { get; init; }
    public string? Search { get; init; }
}

public class QualityViewModel
{
    public IReadOnlyList<DocumentDto> Documents { get; init; } = Array.Empty<DocumentDto>();
}

public class ContactViewModel
{
    public ContactFormModel Form { get; set; } = new();
    public SiteSettingsDto Settings { get; set; } = new();
}

public class CareersViewModel
{
    public JobApplicationFormModel Form { get; set; } = new();
    public string AllowedExtensions { get; } = string.Join(",", FileStorageService.AllowedResumeExtensions);
}

public class QuoteViewModel
{
    public QuoteFormModel Form { get; set; } = new();
    public IReadOnlyList<ProductOptionDto> Products { get; set; } = Array.Empty<ProductOptionDto>();
    public string AllowedExtensions { get; set; } = string.Empty;
}

public class ErrorViewModel
{
    public int StatusCode { get; init; }
}
