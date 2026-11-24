using PrjProje.Domain.Enums;

namespace PrjProje.Application.Models;

public record ProductCategoryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string? Image,
    string IconClass,
    bool IsRestricted,
    int ProductCount,
    string? MetaTitle,
    string? MetaDescription,
    IReadOnlyDictionary<string, string> AlternateSlugs);

public record ProductListItemDto(
    int Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Image,
    string CategoryName,
    string CategorySlug,
    string CategoryIcon);

public class ProductFilter
{
    public string? CategorySlug { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public record SpecificationDto(string Name, string Value);

public record ProductImageDto(string Path, string Alt);

public class ProductDetailDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? Description { get; init; }
    /// <summary>Model codes in display order, e.g. "B 300", "YB 500".</summary>
    public IReadOnlyList<string> Variants { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ApplicationAreas { get; init; } = Array.Empty<string>();
    public string? MetaDescription { get; init; }
    public string? Image { get; init; }
    public IReadOnlyList<ProductImageDto> Images { get; init; } = Array.Empty<ProductImageDto>();
    public IReadOnlyList<SpecificationDto> Specifications { get; init; } = Array.Empty<SpecificationDto>();
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;
    public string CategoryIcon { get; init; } = string.Empty;
    public bool IsRestricted { get; init; }
    public DateTime LastModified { get; init; }
    public IReadOnlyDictionary<string, string> AlternateSlugs { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<ProductListItemDto> RelatedProducts { get; init; } = Array.Empty<ProductListItemDto>();
}

public record ProductOptionDto(int Id, string Name);

public record BlogCategoryDto(int Id, string Name, string Slug, int PostCount);

public record TagDto(string Name, string Slug);

public record BlogPostListItemDto(
    int Id,
    string Title,
    string Slug,
    string? Summary,
    string? CoverImage,
    string? CategoryName,
    string? CategorySlug,
    string AuthorName,
    DateTime PublishedDate);

public class BlogFilter
{
    public string? CategorySlug { get; set; }
    public string? TagSlug { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}

public class BlogPostDetailDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? CoverImage { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? CategoryName { get; init; }
    public string? CategorySlug { get; init; }
    public DateTime PublishedDate { get; init; }
    public DateTime LastModified { get; init; }
    public IReadOnlyList<TagDto> Tags { get; init; } = Array.Empty<TagDto>();
    public IReadOnlyDictionary<string, string> AlternateSlugs { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<BlogPostListItemDto> RelatedPosts { get; init; } = Array.Empty<BlogPostListItemDto>();
}

public record GalleryCategoryDto(int Id, string Name, string Slug, int ItemCount);

public record GalleryItemDto(int Id, int CategoryId, string Title, string ImagePath);

public record GalleryDto(IReadOnlyList<GalleryCategoryDto> Categories, IReadOnlyList<GalleryItemDto> Items, IReadOnlyList<VideoDto> Videos)
{
    public bool IsEmpty => Items.Count == 0 && Videos.Count == 0;
}

public record VideoDto(string YouTubeId, string Title);

public record DocumentDto(
    int Id,
    string Title,
    string? Description,
    string FilePath,
    long FileSize,
    DocumentType DocumentType,
    DateTime DocumentDate,
    string? CoverImagePath);

public class ServiceDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Content { get; init; }
    public string IconClass { get; init; } = string.Empty;
    public string? Image { get; init; }
    public int? GalleryCategoryId { get; init; }
    public IReadOnlyList<int> ProductCategoryIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<SpecificationDto> Specifications { get; init; } = Array.Empty<SpecificationDto>();
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public IReadOnlyDictionary<string, string> AlternateSlugs { get; init; } = new Dictionary<string, string>();
}

public record SitemapEntry(string PageKey, IReadOnlyDictionary<string, string> Slugs, DateTime LastModified);

public record ReferenceDto(string Name, string Logo, string? WebsiteUrl);

/// <summary><see cref="Version"/> changes whenever the announcement is edited, so a visitor who closed it sees the new version.</summary>
public record AnnouncementDto(int Id, AnnouncementDisplay DisplayType, string Message, string? LinkUrl, string? LinkText, string? Image, string Version);
