using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

public class Service : BaseEntity, ITranslatable<ServiceTranslation>
{
    public string IconClass { get; set; } = "fa-solid fa-gear";
    public string? Image { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowOnHome { get; set; } = true;
    public int? GalleryCategoryId { get; set; }
    public GalleryCategory? GalleryCategory { get; set; }
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    public ICollection<ServiceTranslation> Translations { get; set; } = new List<ServiceTranslation>();
}

public class ServiceTranslation : ITranslation
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? Specifications { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
