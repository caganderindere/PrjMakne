using System.ComponentModel.DataAnnotations;
using PrjProje.Application.Common;
using PrjProje.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PrjProje.Application.Models.Admin;

public record OptionItem(int Id, string Name);

public static class TranslationValidation
{
    /// <summary>
    /// The default language is mandatory; other languages are optional but, when started, need a name/title.
    /// </summary>
    public static IEnumerable<ValidationResult> Validate<T>(
        IList<T> translations,
        Func<T, string> languageSelector,
        Func<T, string?> requiredValueSelector,
        string fieldName,
        string label)
    {
        var defaultTranslation = translations.FirstOrDefault(t => languageSelector(t) == Languages.Default);
        if (defaultTranslation is null || string.IsNullOrWhiteSpace(requiredValueSelector(defaultTranslation)))
        {
            var index = defaultTranslation is null ? 0 : translations.IndexOf(defaultTranslation);
            yield return new ValidationResult(
                $"{Languages.DisplayName(Languages.Default)} {label} zorunludur.",
                new[] { $"Translations[{index}].{fieldName}" });
        }
    }
}

public class CategoryTranslationInput
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Kategori Adı")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Name { get; set; }

    [Display(Name = "Slug")]
    [StringLength(180, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Slug { get; set; }

    [Display(Name = "Açıklama")]
    [StringLength(1000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Description { get; set; }

    [Display(Name = "SEO Başlığı")]
    [StringLength(70, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    [StringLength(170, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? MetaDescription { get; set; }
}

public class ProductCategoryEditModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "İkon (Font Awesome sınıfı)")]
    [StringLength(100)]
    public string IconClass { get; set; } = "fa-solid fa-gears";

    public string? Image { get; set; }

    [Display(Name = "Kategori Görseli")]
    public IFormFile? ImageFile { get; set; }

    public bool RemoveImage { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Kısıtlı kategori (teknik özellikler sitede gösterilmez)")]
    public bool IsRestricted { get; set; }

    public List<CategoryTranslationInput> Translations { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        TranslationValidation.Validate(Translations, t => t.LanguageCode, t => t.Name, nameof(CategoryTranslationInput.Name), "kategori adı");
}

public class ProductTranslationInput
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Ürün Adı")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Name { get; set; }

    [Display(Name = "Slug")]
    [StringLength(220, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Slug { get; set; }

    [Display(Name = "Kısa Açıklama")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Detaylı Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Kullanım Alanları (her satıra bir alan)")]
    [StringLength(2000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? ApplicationAreas { get; set; }

    [Display(Name = "Teknik Özellikler (her satıra 'Özellik: Değer')")]
    [StringLength(5000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? SpecificationsText { get; set; }

    [Display(Name = "Meta Açıklama")]
    [StringLength(170, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? MetaDescription { get; set; }
}

public class ProductImageItem
{
    public int Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Delete { get; set; }
}

public class ProductEditModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Kategori")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kategori seçin.")]
    public int CategoryId { get; set; }

    /// <summary>Model codes in display order ("B 300", "YB 500"); the same in every language.</summary>
    [Display(Name = "Modeller / Varyantlar")]
    public List<string?> Variants { get; set; } = new();

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Öne Çıkan")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    public string? Image { get; set; }

    [Display(Name = "Ana Görsel")]
    public IFormFile? ImageFile { get; set; }

    public bool RemoveImage { get; set; }

    [Display(Name = "Galeri Görselleri")]
    public List<IFormFile>? GalleryFiles { get; set; }

    public List<ProductImageItem> ExistingImages { get; set; } = new();

    public List<ProductTranslationInput> Translations { get; set; } = new();

    [BindNever]
    public IReadOnlyList<OptionItem> CategoryOptions { get; set; } = Array.Empty<OptionItem>();

    [BindNever]
    public IReadOnlyList<int> RestrictedCategoryIds { get; set; } = Array.Empty<int>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in TranslationValidation.Validate(Translations, t => t.LanguageCode, t => t.Name, nameof(ProductTranslationInput.Name), "ürün adı"))
        {
            yield return result;
        }

        if (Variants.Any(v => v?.Trim().Length > ProductVariantMaxLength))
        {
            yield return new ValidationResult($"Model kodu en fazla {ProductVariantMaxLength} karakter olabilir.", new[] { nameof(Variants) });
        }
    }

    public const int ProductVariantMaxLength = 50;
}

public class BlogTranslationInput
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Başlık")]
    [StringLength(250, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Title { get; set; }

    [Display(Name = "Slug")]
    [StringLength(270, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Slug { get; set; }

    [Display(Name = "Özet")]
    [StringLength(600, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Summary { get; set; }

    [Display(Name = "İçerik")]
    public string? Content { get; set; }

    [Display(Name = "Etiketler (virgülle ayırın)")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? TagsText { get; set; }

    [Display(Name = "SEO Başlığı")]
    [StringLength(70, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    [StringLength(170, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? MetaDescription { get; set; }

    [Display(Name = "Durum")]
    public PublishStatus Status { get; set; } = PublishStatus.Draft;

    [Display(Name = "Yayın Tarihi")]
    public DateTime? PublishedDate { get; set; }
}

public class BlogPostEditModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    [Display(Name = "Yazar")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string AuthorName { get; set; } = string.Empty;

    public string? CoverImage { get; set; }

    [Display(Name = "Kapak Görseli")]
    public IFormFile? CoverImageFile { get; set; }

    public bool RemoveCover { get; set; }

    public List<BlogTranslationInput> Translations { get; set; } = new();

    [BindNever]
    public IReadOnlyList<OptionItem> CategoryOptions { get; set; } = Array.Empty<OptionItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var anyTitle = Translations.Any(t => !string.IsNullOrWhiteSpace(t.Title));
        if (!anyTitle)
        {
            yield return new ValidationResult("En az bir dilde başlık girilmelidir.", new[] { "Translations[0].Title" });
        }

        for (var i = 0; i < Translations.Count; i++)
        {
            var translation = Translations[i];
            if (!string.IsNullOrWhiteSpace(translation.Title) && string.IsNullOrWhiteSpace(translation.Content))
            {
                yield return new ValidationResult(
                    $"{Languages.DisplayName(translation.LanguageCode)} içerik alanı zorunludur.",
                    new[] { $"Translations[{i}].Content" });
            }
        }
    }
}

public class SimpleTranslationInput
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Ad")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Name { get; set; }

    [Display(Name = "Slug")]
    [StringLength(180, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Slug { get; set; }
}

public class SimpleCategoryEditModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    public List<SimpleTranslationInput> Translations { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        TranslationValidation.Validate(Translations, t => t.LanguageCode, t => t.Name, nameof(SimpleTranslationInput.Name), "ad");
}

public record SimpleCategoryListItem(int Id, string Name, int SortOrder, int ItemCount);

public class GalleryTitleInput
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Başlık")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Title { get; set; }
}

public class GalleryUploadModel
{
    [Display(Name = "Kategori")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kategori seçin.")]
    public int CategoryId { get; set; }

    [Display(Name = "Fotoğraflar")]
    public List<IFormFile> Files { get; set; } = new();

    public List<GalleryTitleInput> Titles { get; set; } = new();
}

public class GalleryItemEditModel
{
    public int Id { get; set; }

    [Display(Name = "Kategori")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir kategori seçin.")]
    public int CategoryId { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public string ImagePath { get; set; } = string.Empty;

    [Display(Name = "Görseli Değiştir")]
    public IFormFile? ImageFile { get; set; }

    public List<GalleryTitleInput> Titles { get; set; } = new();

    [BindNever]
    public IReadOnlyList<OptionItem> CategoryOptions { get; set; } = Array.Empty<OptionItem>();
}

public record GalleryAdminItem(int Id, string ImagePath, string Title, string CategoryName, int SortOrder, bool IsActive);

public class GallerySortInput
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
}

public class DocumentEditModel
{
    public int Id { get; set; }

    [Display(Name = "Belge Adı")]
    [Required(ErrorMessage = "{0} zorunludur.")]
    [StringLength(200, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [StringLength(1000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Description { get; set; }

    [Display(Name = "Belge Türü")]
    public DocumentType DocumentType { get; set; }

    [Display(Name = "Dil")]
    public string? LanguageCode { get; set; }

    [Display(Name = "Belge Tarihi")]
    [DataType(DataType.Date)]
    public DateTime DocumentDate { get; set; } = DateTime.Today;

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Yayında")]
    public bool IsActive { get; set; } = true;

    public string? FilePath { get; set; }

    [Display(Name = "PDF Dosyası")]
    public IFormFile? File { get; set; }

    public string? CoverImagePath { get; set; }

    [Display(Name = "Kapak Görseli")]
    public IFormFile? CoverImage { get; set; }
}

public record DocumentAdminItem(int Id, string Title, DocumentType DocumentType, string? LanguageCode, DateTime DocumentDate, long FileSize, string FilePath, bool IsActive);

public class ServiceTranslationInput
{
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Başlık")]
    [StringLength(150, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Title { get; set; }

    [Display(Name = "Slug")]
    [StringLength(180, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Slug { get; set; }

    [Display(Name = "Kısa Açıklama")]
    [StringLength(500, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Summary { get; set; }

    [Display(Name = "Detay İçerik")]
    public string? Content { get; set; }

    [Display(Name = "Teknik Kapasite (her satıra 'Özellik: Değer')")]
    [StringLength(3000, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? Specifications { get; set; }

    [Display(Name = "SEO Başlığı")]
    [StringLength(70, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? MetaTitle { get; set; }

    [Display(Name = "Meta Açıklama")]
    [StringLength(170, ErrorMessage = "{0} en fazla {1} karakter olabilir.")]
    public string? MetaDescription { get; set; }
}

public class ServiceEditModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "İkon (Font Awesome sınıfı)")]
    [StringLength(100)]
    public string IconClass { get; set; } = "fa-solid fa-gear";

    public string? Image { get; set; }

    [Display(Name = "Görsel")]
    public IFormFile? ImageFile { get; set; }

    public bool RemoveImage { get; set; }

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Ana sayfada göster")]
    public bool ShowOnHome { get; set; } = true;

    [Display(Name = "Galeri Kategorisi")]
    public int? GalleryCategoryId { get; set; }

    [BindNever]
    public IReadOnlyList<OptionItem> GalleryOptions { get; set; } = Array.Empty<OptionItem>();

    public List<int> ProductCategoryIds { get; set; } = new();

    [BindNever]
    public IReadOnlyList<OptionItem> CategoryOptions { get; set; } = Array.Empty<OptionItem>();

    public List<ServiceTranslationInput> Translations { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        TranslationValidation.Validate(Translations, t => t.LanguageCode, t => t.Title, nameof(ServiceTranslationInput.Title), "başlık");
}

public record TranslatedListItem(int Id, string Name, string? Image, int SortOrder, bool IsActive, IReadOnlyList<string> Languages, DateTime? UpdatedDate);

public record ProductAdminListItem(
    int Id,
    string Name,
    string CategoryName,
    string? Image,
    int SortOrder,
    bool IsActive,
    bool IsFeatured,
    IReadOnlyList<string> Languages,
    DateTime CreatedDate);

public class AdminListFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public record BlogAdminListItem(
    int Id,
    string Title,
    string? CategoryName,
    string AuthorName,
    string? CoverImage,
    IReadOnlyDictionary<string, PublishStatus> Statuses,
    DateTime CreatedDate);
