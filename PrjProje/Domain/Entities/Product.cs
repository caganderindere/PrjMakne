using PrjProje.Domain.Common;

namespace PrjProje.Domain.Entities;

public class Product : BaseEntity, ITranslatable<ProductTranslation>
{
    public int CategoryId { get; set; }
    public ProductCategory Category { get; set; } = null!;
    public string? Image { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    public ICollection<ProductTranslation> Translations { get; set; } = new List<ProductTranslation>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}

public class ProductTranslation : ITranslation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? ApplicationAreas { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>A model of the product, such as "B 300" or "YB 500". Codes are the same in every language.</summary>
public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class ProductSpecification
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
