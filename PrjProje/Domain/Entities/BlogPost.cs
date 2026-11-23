using PrjProje.Domain.Common;
using PrjProje.Domain.Enums;

namespace PrjProje.Domain.Entities;

public class BlogCategory : BaseEntity, ITranslatable<BlogCategoryTranslation>
{
    public int SortOrder { get; set; }
    public ICollection<BlogCategoryTranslation> Translations { get; set; } = new List<BlogCategoryTranslation>();
    public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}

public class BlogCategoryTranslation : ITranslation
{
    public int Id { get; set; }
    public int BlogCategoryId { get; set; }
    public BlogCategory BlogCategory { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class BlogPost : BaseEntity, ITranslatable<BlogPostTranslation>
{
    public int? CategoryId { get; set; }
    public BlogCategory? Category { get; set; }
    public string? CoverImage { get; set; }
    public string AuthorName { get; set; } = string.Empty;

    public ICollection<BlogPostTranslation> Translations { get; set; } = new List<BlogPostTranslation>();
}

/// <summary>
/// Each language version of a post has its own publish status and date,
/// so Turkish and English content can be managed independently.
/// </summary>
public class BlogPostTranslation : ITranslation
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public DateTime? PublishedDate { get; set; }

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

public class Tag
{
    public int Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<BlogPostTranslation> Posts { get; set; } = new List<BlogPostTranslation>();
}
