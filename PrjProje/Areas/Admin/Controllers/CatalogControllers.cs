using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Application.Models.Admin;
using PrjProje.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace PrjProje.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IDeveloperService _developerService;

    public DashboardController(IDashboardService dashboardService, IDeveloperService developerService)
    {
        _dashboardService = dashboardService;
        _developerService = developerService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (Roles.HasFullAccess(User))
        {
            ViewBag.HealthChecks = await _developerService.GetChecksAsync(cancellationToken);
        }

        return View(await _dashboardService.GetAsync(cancellationToken));
    }
}

public class ProductsController : AdminControllerBase
{
    private readonly IProductAdminService _productService;
    private readonly IProductCategoryAdminService _categoryService;

    public ProductsController(IProductAdminService productService, IProductCategoryAdminService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index([FromQuery] AdminListFilter filter, CancellationToken cancellationToken)
    {
        ViewBag.Filter = filter;
        ViewBag.Categories = await _categoryService.GetOptionsAsync(cancellationToken);
        return View(await _productService.GetListAsync(filter, cancellationToken));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View("Form", await _productService.CreateNewAsync(cancellationToken));

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _productService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Save(ProductEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _productService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Ürün kaydedildi.");
                return RedirectToAction(nameof(Edit), new { id = result.Value });
            }

            AddErrors(result);
        }

        await _productService.PopulateOptionsAsync(model, cancellationToken);
        if (model.Id > 0)
        {
            model.ExistingImages = (await _productService.GetForEditAsync(model.Id, cancellationToken))?.ExistingImages ?? model.ExistingImages;
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _productService.ToggleActiveAsync(id, cancellationToken), "Ürün durumu güncellendi.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _productService.DeleteAsync(id, cancellationToken), "Ürün silindi.");
}

public class ProductCategoriesController : AdminControllerBase
{
    private readonly IProductCategoryAdminService _categoryService;

    public ProductCategoriesController(IProductCategoryAdminService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _categoryService.GetListAsync(cancellationToken));

    public IActionResult Create() => View("Form", _categoryService.CreateNew());

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _categoryService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProductCategoryEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _categoryService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Kategori kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _categoryService.DeleteAsync(id, cancellationToken), "Kategori silindi.");
}

public class CapabilitiesController : AdminControllerBase
{
    private readonly ICapabilityAdminService _capabilityService;

    public CapabilitiesController(ICapabilityAdminService capabilityService)
    {
        _capabilityService = capabilityService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _capabilityService.GetListAsync(cancellationToken));

    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View("Form", await _capabilityService.CreateNewAsync(cancellationToken));

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _capabilityService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CapabilityEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _capabilityService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Üretim kabiliyeti kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        await _capabilityService.PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _capabilityService.DeleteAsync(id, cancellationToken), "Üretim kabiliyeti silindi.");
}

public class BlogController : AdminControllerBase
{
    private readonly IBlogAdminService _blogService;
    private readonly IBlogCategoryAdminService _categoryService;

    public BlogController(IBlogAdminService blogService, IBlogCategoryAdminService categoryService)
    {
        _blogService = blogService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index([FromQuery] AdminListFilter filter, CancellationToken cancellationToken)
    {
        ViewBag.Filter = filter;
        ViewBag.Categories = await _categoryService.GetOptionsAsync(cancellationToken);
        return View(await _blogService.GetListAsync(filter, cancellationToken));
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var author = User.Identity?.Name ?? "PRJ Proje Makina";
        return View("Form", await _blogService.CreateNewAsync(author, cancellationToken));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _blogService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Save(BlogPostEditModel model, bool publish, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            if (publish)
            {
                foreach (var translation in model.Translations)
                {
                    translation.Status = PublishStatus.Published;
                }
            }

            var result = await _blogService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success(publish ? "Yazı yayınlandı." : "Blog yazısı kaydedildi.");
                return RedirectToAction(nameof(Edit), new { id = result.Value });
            }

            AddErrors(result);
        }

        await _blogService.PopulateOptionsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, PublishStatus status, CancellationToken cancellationToken) =>
        ResultRedirect(await _blogService.SetStatusAsync(id, status, cancellationToken),
            status == PublishStatus.Published ? "Yazı yayınlandı." : "Yazı taslağa alındı.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _blogService.DeleteAsync(id, cancellationToken), "Blog yazısı silindi.");
}

/// <summary>Shared CRUD for simple translated categories; views live in Admin/Views/Shared.</summary>
public abstract class SimpleCategoryControllerBase : AdminControllerBase
{
    private readonly ISimpleCategoryAdminService _service;

    protected SimpleCategoryControllerBase(ISimpleCategoryAdminService service)
    {
        _service = service;
    }

    protected abstract string PageTitle { get; }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = PageTitle;
        return View("SimpleCategoryIndex", await _service.GetListAsync(cancellationToken));
    }

    public IActionResult Create()
    {
        ViewData["Title"] = PageTitle;
        return View("SimpleCategoryForm", _service.CreateNew());
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = PageTitle;
        var model = await _service.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("SimpleCategoryForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SimpleCategoryEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _service.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Kategori kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        ViewData["Title"] = PageTitle;
        return View("SimpleCategoryForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _service.DeleteAsync(id, cancellationToken), "Kategori silindi.");
}

public class BlogCategoriesController : SimpleCategoryControllerBase
{
    public BlogCategoriesController(IBlogCategoryAdminService service) : base(service)
    {
    }

    protected override string PageTitle => "Blog Kategorileri";
}

public class GalleryCategoriesController : SimpleCategoryControllerBase
{
    public GalleryCategoriesController(IGalleryCategoryAdminService service) : base(service)
    {
    }

    protected override string PageTitle => "Galeri Kategorileri";
}

public class GalleryController : AdminControllerBase
{
    private readonly IGalleryAdminService _galleryService;
    private readonly IGalleryCategoryAdminService _categoryService;

    public GalleryController(IGalleryAdminService galleryService, IGalleryCategoryAdminService categoryService)
    {
        _galleryService = galleryService;
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(int? categoryId, CancellationToken cancellationToken)
    {
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await _categoryService.GetOptionsAsync(cancellationToken);
        ViewBag.Upload = _galleryService.CreateUploadModel();
        return View(await _galleryService.GetListAsync(categoryId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Upload(GalleryUploadModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Error("Lütfen kategori seçip en az bir fotoğraf yükleyin.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _galleryService.UploadAsync(model, cancellationToken);
        return ResultRedirect(result, $"{result.Value} fotoğraf yüklendi.", nameof(Index), new { categoryId = model.CategoryId });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _galleryService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Edit(GalleryItemEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _galleryService.UpdateAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Fotoğraf güncellendi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        await _galleryService.PopulateOptionsAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sort(List<GallerySortInput> items, int? categoryId, CancellationToken cancellationToken) =>
        ResultRedirect(await _galleryService.UpdateSortOrderAsync(items, cancellationToken), "Sıralama kaydedildi.", nameof(Index), new { categoryId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? categoryId, CancellationToken cancellationToken) =>
        ResultRedirect(await _galleryService.DeleteAsync(id, cancellationToken), "Fotoğraf silindi.", nameof(Index), new { categoryId });
}

public class DocumentsController : AdminControllerBase
{
    private readonly IDocumentAdminService _documentService;

    public DocumentsController(IDocumentAdminService documentService)
    {
        _documentService = documentService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await _documentService.GetListAsync(cancellationToken));

    public IActionResult Create() => View("Form", new DocumentEditModel());

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await _documentService.GetForEditAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LargeUploadLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = LargeUploadLimit)]
    public async Task<IActionResult> Save(DocumentEditModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _documentService.SaveAsync(model, cancellationToken);
            if (result.Succeeded)
            {
                Success("Belge kaydedildi.");
                return RedirectToAction(nameof(Index));
            }

            AddErrors(result);
        }

        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        ResultRedirect(await _documentService.DeleteAsync(id, cancellationToken), "Belge silindi.");
}
