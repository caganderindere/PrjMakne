using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

public class ProductCategory : BaseEntity, ITranslatable<ProductCategoryTranslation>
{
    public string? Image { get; set; }
    public string IconClass { get; set; } = "fa-solid fa-gears";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Restricted categories (e.g. confidential customer projects) never expose technical specifications on the public site.
    /// </summary>
    public bool IsRestricted { get; set; }

    public ICollection<ProductCategoryTranslation> Translations { get; set; } = new List<ProductCategoryTranslation>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class ProductCategoryTranslation : ITranslation
{
    public int Id { get; set; }
    public int ProductCategoryId { get; set; }
    public ProductCategory ProductCategory { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
