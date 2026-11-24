using PrjProje.Application.Common;
using PrjProje.Application.Interfaces;
using PrjProje.Domain.Entities;
using PrjProje.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PrjProje.Infrastructure.Data;

/// <summary>
/// Idempotent seeding of roles, the first administrator, site settings and gallery categories.
/// Each block only runs when its table is empty, so admin edits are never overwritten.
/// Catalogue content is loaded from content/seed (see the content seeding step).
/// </summary>
public static class DbSeeder
{
    private const string Tr = Languages.Turkish;
    private const string En = Languages.English;

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

        await db.Database.MigrateAsync();

        await SeedRolesAndAdminAsync(provider, logger);
        await SeedSettingsAsync(db);
        await SeedGalleryCategoriesAsync(db);
        var seedRoot = Path.Combine(AppContext.BaseDirectory, "SeedContent");
        await CatalogSeeder.SeedAsync(db, provider.GetRequiredService<IFileStorageService>(), seedRoot, logger);
        await BlogSeeder.SeedAsync(db, seedRoot);

        provider.GetRequiredService<IContentCache>().Invalidate();
    }

    private static async Task SeedRolesAndAdminAsync(IServiceProvider provider, ILogger logger)
    {
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.Users.AnyAsync())
        {
            return;
        }

        var configuration = provider.GetRequiredService<IConfiguration>();
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No users exist and SeedAdmin:Email / SeedAdmin:Password are not configured. Admin user was not created.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = configuration["SeedAdmin:FullName"] ?? "Site Yöneticisi",
            // The seed password lives in server configuration: it is replaced on the first sign-in.
            MustChangePassword = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation("Seed admin user {Email} created; the password must be changed at the first sign-in.", email);
        }
        else
        {
            logger.LogError("Seed admin user could not be created: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db)
    {
        if (await db.SiteSettings.AnyAsync())
        {
            return;
        }

        db.SiteSettings.AddRange(
            new SiteSetting { Key = "CompanyName", Value = "PRJ Proje Makina A.Ş." },
            new SiteSetting { Key = "Phone", Value = "+90 212 544 01 04" },
            new SiteSetting { Key = "Fax", Value = "+90 212 565 63 20" },
            new SiteSetting { Key = "Gsm", Value = "+90 531 796 00 29" },
            new SiteSetting { Key = "Email", Value = "info@prestijteknikmakine.com.tr" },
            new SiteSetting { Key = "SalesEmail", Value = "satis@prestijteknikmakine.com.tr" },
            new SiteSetting { Key = "AddressTr", Value = "Mehmet Akif Ersoy Mah. Candaner Sok. No:15-A Taşoluk, 34281 Arnavutköy / İstanbul, Türkiye" },
            new SiteSetting { Key = "AddressEn", Value = "Mehmet Akif Ersoy Mah. Candaner Sok. No:15-A Taşoluk, 34281 Arnavutköy / Istanbul, Türkiye" },
            new SiteSetting { Key = "WorkingHoursTr", Value = "Pazartesi - Cuma: 08:30 - 18:00 | Cumartesi: 08:30 - 13:00" },
            new SiteSetting { Key = "WorkingHoursEn", Value = "Monday - Friday: 08:30 - 18:00 | Saturday: 08:30 - 13:00" },
            new SiteSetting { Key = "MapEmbedUrl", Value = "https://www.google.com/maps?q=Ta%C5%9Foluk+Arnavutk%C3%B6y+%C4%B0stanbul+Candaner+Sok.+15&output=embed" },
            new SiteSetting { Key = "InstagramUrl", Value = "https://www.instagram.com/prjprojemakina" },
            new SiteSetting { Key = "FacebookUrl", Value = "https://www.facebook.com/PRJProjeMakine" },
            new SiteSetting { Key = "YouTubeUrl", Value = "https://www.youtube.com/@prjprojemakina" },
            new SiteSetting { Key = "WhatsApp", Value = "905317960029" });

        await db.SaveChangesAsync();
    }

    private static async Task SeedGalleryCategoriesAsync(ApplicationDbContext db)
    {
        if (await db.GalleryCategories.AnyAsync())
        {
            return;
        }

        var names = new (string Tr, string En)[]
        {
            ("Üretim", "Production"), ("Tesis Kurulumları", "Plant Installations"), ("Ürünler", "Products"), ("Fuarlar", "Trade Fairs")
        };

        var order = 1;
        foreach (var (trName, enName) in names)
        {
            db.GalleryCategories.Add(new GalleryCategory
            {
                SortOrder = order++,
                Translations =
                {
                    new GalleryCategoryTranslation { LanguageCode = Tr, Name = trName, Slug = SlugHelper.Generate(trName) },
                    new GalleryCategoryTranslation { LanguageCode = En, Name = enName, Slug = SlugHelper.Generate(enName) }
                }
            });
        }

        await db.SaveChangesAsync();
    }
}
