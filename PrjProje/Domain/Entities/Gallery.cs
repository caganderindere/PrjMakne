using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

public class GalleryCategory : BaseEntity, ITranslatable<GalleryCategoryTranslation>
{
    public int SortOrder { get; set; }
    public ICollection<GalleryCategoryTranslation> Translations { get; set; } = new List<GalleryCategoryTranslation>();
    public ICollection<GalleryItem> Items { get; set; } = new List<GalleryItem>();
}

public class GalleryCategoryTranslation : ITranslation
{
    public int Id { get; set; }
    public int GalleryCategoryId { get; set; }
    public GalleryCategory GalleryCategory { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class GalleryItem : BaseEntity, ITranslatable<GalleryItemTranslation>
{
    public int CategoryId { get; set; }
    public GalleryCategory Category { get; set; } = null!;
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<GalleryItemTranslation> Translations { get; set; } = new List<GalleryItemTranslation>();
}

public class GalleryItemTranslation : ITranslation
{
    public int Id { get; set; }
    public int GalleryItemId { get; set; }
    public GalleryItem GalleryItem { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
