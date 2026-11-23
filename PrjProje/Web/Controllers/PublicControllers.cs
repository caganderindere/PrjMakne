using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models;
using PrjProje.Domain.Enums;
using PrjProje.Web.Infrastructure;
using PrjProje.Web.Routing;
using PrjProje.Web.Seo;
using PrjProje.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Web.Controllers;

public class HomeController : PublicControllerBase
{
    private readonly IProductService _productService;
    private readonly IServicePageService _servicePageService;
    private readonly IBlogService _blogService;
    private readonly ISiteSettingsService _settingsService;
    private readonly ISiteContentService _siteContentService;
    private readonly IWebHostEnvironment _environment;

    public HomeController(
        IProductService productService,
        IServicePageService servicePageService,
        IBlogService blogService,
        ISiteSettingsService settingsService,
        ISiteContentService siteContentService,
        IWebHostEnvironment environment)
    {
        _environment = environment;
        _productService = productService;
        _servicePageService = servicePageService;
        _blogService = blogService;
        _settingsService = settingsService;
        _siteContentService = siteContentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);
        var model = new HomeViewModel
        {
            Categories = await _productService.GetCategoriesAsync(Lang, cancellationToken),
            Services = await _servicePageService.GetServicesAsync(Lang, homeOnly: true, cancellationToken),
            FeaturedProducts = await _productService.GetFeaturedAsync(Lang, 4, cancellationToken),
            LatestPosts = await _blogService.GetLatestAsync(Lang, 3, cancellationToken),
            ExportCountryCodes = settings.GetExportCountryCodes(),
            References = await _siteContentService.GetReferencesAsync(cancellationToken),
            HeroImage = settings.HeroImage,
            HeroVideo = ExistingFile(settings.HeroVideo),
            HeroVideoMobile = ExistingFile(settings.HeroVideoMobile),
            Sections = settings.GetHomeSections().Where(s => s.Visible).Select(s => s.Key).ToList(),
            Stats = HomeStats.From(settings)
        };

        SetSeo(L["Home.MetaTitle"], L["Home.MetaDescription"], SiteRoutes.Home);
        return View(model);
    }

    // A video whose file is missing must not leave phones with an empty hero: the photo and text stay instead.
    private string? ExistingFile(string? path) =>
        !string.IsNullOrWhiteSpace(path) && _environment.WebRootFileProvider.GetFileInfo(path).Exists ? path : null;
}

public class AboutController : PublicControllerBase
{
    public IActionResult Index()
    {
        var seo = SetSeo(L["About.MetaTitle"], L["About.MetaDescription"], SiteRoutes.About);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.About"], null));
        return View();
    }
}

public class ProductsController : PublicControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(string? slug, string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        var categories = await _productService.GetCategoriesAsync(Lang, cancellationToken);
        ProductCategoryDto? category = null;
        if (!string.IsNullOrWhiteSpace(slug))
        {
            category = categories.FirstOrDefault(c => c.Slug == slug);
            if (category is null)
            {
                return NotFound();
            }
        }

        var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 100)];
        var products = await _productService.GetProductsAsync(Lang, new ProductFilter
        {
            CategorySlug = category?.Slug,
            Search = search,
            Page = page
        }, cancellationToken);

        PageSeo seo;
        if (category is not null)
        {
            seo = SetSeo(category.MetaTitle ?? $"{category.Name} | {L["Nav.Products"]}",
                category.MetaDescription ?? category.Description, SiteRoutes.ProductCategory, category.AlternateSlugs, SiteRoutes.Products);
            seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Products"], Url.Localized(SiteRoutes.Products)));
            seo.Breadcrumbs.Add(new BreadcrumbItem(category.Name, null));
        }
        else
        {
            seo = SetSeo(L["Products.MetaTitle"], L["Products.MetaDescription"], SiteRoutes.Products);
            seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Products"], null));
        }

        if (search is not null || page > 1)
        {
            seo.NoIndex = search is not null;
            if (page > 1)
            {
                seo.CanonicalPath += $"?page={page}";
            }
        }

        return View(new ProductListViewModel
        {
            Categories = categories,
            CurrentCategory = category,
            Search = search,
            Products = products
        });
    }

    /// <summary>Type-ahead for the product search box: the five most relevant products as JSON.</summary>
    [HttpGet("api/products/suggest")]
    public async Task<IActionResult> Suggest(string? q, string? lang, string? category, CancellationToken cancellationToken)
    {
        var languageCode = Languages.IsSupported(lang) ? lang! : Languages.Default;
        var term = q?.Trim() ?? string.Empty;
        if (term.Length < 2)
        {
            return Json(Array.Empty<object>());
        }

        var products = await _productService.GetProductsAsync(languageCode, new ProductFilter
        {
            CategorySlug = category,
            Search = term[..Math.Min(term.Length, 100)],
            PageSize = 5
        }, cancellationToken);

        return Json(products.Items.Select(p => new
        {
            p.Name,
            p.CategoryName,
            Url = SiteRoutes.Path(SiteRoutes.ProductDetail, languageCode, p.Slug),
            Image = p.Image is null ? null : ViewHelpers.SmallImage(p.Image)
        }));
    }

    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var product = await _productService.GetDetailAsync(Lang, slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        // Suppliers search Google by part number: the codes lead the description. The title is the name as entered
        // (OEM no + name), and _SeoHead appends "| PRJ Proje Makina".
        string[] SplitCodes(string? codes) =>
            codes?.Split([',', ';', '/', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var oemNumbers = SplitCodes(product.OemNumber);
        var serviceCodes = SplitCodes(product.ServiceCode);
        var codeLines = new List<string>();
        if (oemNumbers.Length > 0)
        {
            codeLines.Add($"{L["Products.OemNumber"]}: {string.Join(", ", oemNumbers)}.");
        }

        if (serviceCodes.Length > 0)
        {
            codeLines.Add($"{L["Products.ServiceCode"]}: {string.Join(", ", serviceCodes)}.");
        }

        var seo = SetSeo(
            product.Name,
            string.Join(" ", codeLines.Append(product.MetaDescription ?? product.ShortDescription ?? string.Empty)).Trim(),
            SiteRoutes.ProductDetail, product.AlternateSlugs, SiteRoutes.Products);
        seo.Image = product.Image;
        seo.OpenGraphType = "product";
        seo.ModifiedTime = product.LastModified;
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Products"], Url.Localized(SiteRoutes.Products)));
        if (!string.IsNullOrEmpty(product.CategorySlug))
        {
            seo.Breadcrumbs.Add(new BreadcrumbItem(product.CategoryName, Url.Localized(SiteRoutes.ProductCategory, product.CategorySlug)));
        }

        seo.Breadcrumbs.Add(new BreadcrumbItem(product.Name, null));
        seo.StructuredData.Add(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = product.Name,
            ["description"] = product.ShortDescription,
            ["image"] = product.Image is null ? null : Url.Absolute(product.Image),
            ["category"] = product.CategoryName,
            ["material"] = product.Material,
            ["mpn"] = product.OemNumber,
            ["sku"] = serviceCodes.FirstOrDefault(),
            ["brand"] = new Dictionary<string, object> { ["@type"] = "Brand", ["name"] = "PRJ" },
            ["manufacturer"] = new Dictionary<string, object> { ["@type"] = "Organization", ["name"] = "PRJ Proje Makina" },
            ["url"] = Url.Absolute(seo.CanonicalPath)
        });

        return View(product);
    }
}

public class ServicesController : PublicControllerBase
{
    private readonly IServicePageService _servicePageService;
    private readonly IGalleryService _galleryService;
    private readonly IProductService _productService;
    private readonly IDocumentService _documentService;

    public ServicesController(IServicePageService servicePageService, IGalleryService galleryService, IProductService productService, IDocumentService documentService)
    {
        _servicePageService = servicePageService;
        _galleryService = galleryService;
        _productService = productService;
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var services = await _servicePageService.GetServicesAsync(Lang, cancellationToken: cancellationToken);
        var seo = SetSeo(L["Services.MetaTitle"], L["Services.MetaDescription"], SiteRoutes.Services);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Services"], null));
        return View(new ServicesViewModel { Services = services });
    }

    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var service = await _servicePageService.GetDetailAsync(Lang, slug, cancellationToken);
        if (service is null)
        {
            return NotFound();
        }

        var all = await _servicePageService.GetServicesAsync(Lang, cancellationToken: cancellationToken);
        var seo = SetSeo(service.MetaTitle ?? $"{service.Title} | {L["Nav.Services"]}",
            service.MetaDescription ?? service.Summary, SiteRoutes.ServiceDetail, service.AlternateSlugs, SiteRoutes.Services);
        seo.Image = service.Image;
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Services"], Url.Localized(SiteRoutes.Services)));
        seo.Breadcrumbs.Add(new BreadcrumbItem(service.Title, null));

        var gallery = service.GalleryCategoryId is { } galleryCategoryId
            ? (await _galleryService.GetGalleryAsync(Lang, cancellationToken)).Items.Where(i => i.CategoryId == galleryCategoryId).ToList()
            : new List<GalleryItemDto>();
        var categories = service.ProductCategoryIds.Count == 0
            ? new List<ProductCategoryDto>()
            : (await _productService.GetCategoriesAsync(Lang, cancellationToken)).Where(c => service.ProductCategoryIds.Contains(c.Id)).ToList();
        var certificates = (await _documentService.GetPublicDocumentsAsync(Lang, cancellationToken))
            .Where(d => d.DocumentType is DocumentType.Certificate or DocumentType.Authorization)
            .ToList();

        return View(new ServiceDetailViewModel
        {
            Service = service,
            Others = all.Where(c => c.Id != service.Id).ToList(),
            Gallery = gallery,
            Categories = categories,
            Certificates = certificates
        });
    }
}

public class BlogController : PublicControllerBase
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    public async Task<IActionResult> Index(string? category, string? tag, string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim()[..Math.Min(q.Trim().Length, 100)];
        var currentTag = string.IsNullOrWhiteSpace(tag) ? null : await _blogService.GetTagAsync(Lang, tag, cancellationToken);
        var posts = await _blogService.GetPostsAsync(Lang, new BlogFilter
        {
            CategorySlug = category,
            TagSlug = currentTag?.Slug,
            Search = search,
            Page = page
        }, cancellationToken);

        var seo = SetSeo(L["Blog.MetaTitle"], L["Blog.MetaDescription"], SiteRoutes.Blog);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Blog"], null));
        seo.NoIndex = search is not null || currentTag is not null || !string.IsNullOrWhiteSpace(category);
        if (page > 1)
        {
            seo.CanonicalPath += $"?page={page}";
        }

        return View(new BlogListViewModel
        {
            Posts = posts,
            Categories = await _blogService.GetCategoriesAsync(Lang, cancellationToken),
            CurrentCategorySlug = category,
            CurrentTag = currentTag,
            Search = search
        });
    }

    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
    {
        var post = await _blogService.GetDetailAsync(Lang, slug, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        var seo = SetSeo(post.MetaTitle ?? post.Title, post.MetaDescription ?? post.Summary, SiteRoutes.BlogDetail, post.AlternateSlugs, SiteRoutes.Blog);
        seo.Image = post.CoverImage;
        seo.OpenGraphType = "article";
        seo.PublishedTime = post.PublishedDate;
        seo.ModifiedTime = post.LastModified;
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Blog"], Url.Localized(SiteRoutes.Blog)));
        seo.Breadcrumbs.Add(new BreadcrumbItem(post.Title, null));
        seo.StructuredData.Add(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BlogPosting",
            ["headline"] = post.Title,
            ["description"] = post.Summary,
            ["image"] = post.CoverImage is null ? null : Url.Absolute(post.CoverImage),
            ["datePublished"] = post.PublishedDate.ToString("O"),
            ["dateModified"] = post.LastModified.ToString("O"),
            ["inLanguage"] = Lang,
            ["author"] = new Dictionary<string, object> { ["@type"] = "Organization", ["name"] = post.AuthorName },
            ["publisher"] = new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"] = "PRJ Proje Makina",
                ["logo"] = new Dictionary<string, object> { ["@type"] = "ImageObject", ["url"] = Url.Absolute("/images/logo.png") }
            },
            ["mainEntityOfPage"] = Url.Absolute(seo.CanonicalPath)
        });

        return View(post);
    }
}

public class VideosController : PublicControllerBase
{
    private readonly IGalleryService _galleryService;

    public VideosController(IGalleryService galleryService)
    {
        _galleryService = galleryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var gallery = await _galleryService.GetGalleryAsync(Lang, cancellationToken);
        var seo = SetSeo(L["Videos.MetaTitle"], L["Videos.MetaDescription"], SiteRoutes.Videos);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Videos"], null));
        return View(gallery);
    }
}

public class CertificatesController : PublicControllerBase
{
    private readonly IDocumentService _documentService;

    public CertificatesController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = (await _documentService.GetPublicDocumentsAsync(Lang, cancellationToken))
            .Where(d => d.DocumentType != DocumentType.Catalog)
            .ToList();
        var seo = SetSeo(L["Certificates.MetaTitle"], L["Certificates.MetaDescription"], SiteRoutes.Certificates);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Certificates"], null));
        return View(new CertificatesViewModel { Documents = documents });
    }
}

public class ManualsController : PublicControllerBase
{
    private readonly IDocumentService _documentService;

    public ManualsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var catalogs = (await _documentService.GetPublicDocumentsAsync(Lang, cancellationToken))
            .Where(d => d.DocumentType == DocumentType.Catalog)
            .ToList();
        var seo = SetSeo(L["Manuals.MetaTitle"], L["Manuals.MetaDescription"], SiteRoutes.Manuals);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Products"], Url.Localized(SiteRoutes.Products)));
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Nav.Manuals"], null));
        return View(catalogs);
    }
}

public class LegalController : PublicControllerBase
{
    public IActionResult Privacy()
    {
        var seo = SetSeo(L["Legal.PrivacyTitle"], L["Legal.PrivacyMetaDescription"], SiteRoutes.Privacy);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Legal.PrivacyTitle"], null));
        return View();
    }

    public IActionResult Cookies()
    {
        var seo = SetSeo(L["Legal.CookiesTitle"], L["Legal.CookiesMetaDescription"], SiteRoutes.Cookies);
        seo.Breadcrumbs.Add(new BreadcrumbItem(L["Legal.CookiesTitle"], null));
        return View();
    }
}
