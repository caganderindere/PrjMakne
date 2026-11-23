using PrjProje.Application.Interfaces;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data;
using PrjProje.Infrastructure.Services;
using PrjProje.Infrastructure.Services.Admin;
using PrjProje.Infrastructure.Services.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PrjProje.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            }));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<PanelClaimsFactory>();

        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddSingleton<IContentCache, ContentCache>();
        services.AddSingleton<IHtmlSanitizerService, HtmlSanitizerService>();
        services.AddSingleton<ISpamProtectionService, SpamProtectionService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddSingleton<IContentTextStore, ContentTextStore>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IGalleryService, GalleryService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ICapabilityService, CapabilityService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<ISiteContentService, SiteContentService>();
        services.AddScoped<INotificationService, EmailNotificationService>();

        services.AddScoped<IProductAdminService, ProductAdminService>();
        services.AddScoped<IProductCategoryAdminService, ProductCategoryAdminService>();
        services.AddScoped<IBlogAdminService, BlogAdminService>();
        services.AddScoped<IBlogCategoryAdminService, BlogCategoryAdminService>();
        services.AddScoped<IGalleryCategoryAdminService, GalleryCategoryAdminService>();
        services.AddScoped<IGalleryAdminService, GalleryAdminService>();
        services.AddScoped<IDocumentAdminService, DocumentAdminService>();
        services.AddScoped<ICapabilityAdminService, CapabilityAdminService>();
        services.AddScoped<IContactAdminService, ContactAdminService>();
        services.AddScoped<IQuoteAdminService, QuoteAdminService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<RecentLogStore>();
        services.AddSingleton<ILoggerProvider>(provider => provider.GetRequiredService<RecentLogStore>());
        services.AddScoped<IReferenceAdminService, ReferenceAdminService>();
        services.AddScoped<IAnnouncementAdminService, AnnouncementAdminService>();
        services.AddScoped<IContentTextAdminService, ContentTextAdminService>();

        return services;
    }
}
