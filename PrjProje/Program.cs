using System.Globalization;
using System.Threading.RateLimiting;
using PrjProje;
using PrjProje.Application.Common;
using PrjProje.Infrastructure;
using PrjProje.Infrastructure.Data;
using PrjProje.Web.Controllers;
using PrjProje.Web.Infrastructure;
using PrjProje.Web.Localization;
using PrjProje.Web.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
// Texts edited in the admin panel override the .resx texts (see ContentOverrideLocalizerFactory).
builder.Services.AddSingleton<ResourceManagerStringLocalizerFactory>();
builder.Services.Replace(ServiceDescriptor.Singleton<IStringLocalizerFactory, ContentOverrideLocalizerFactory>());
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = Languages.All.Select(code => new CultureInfo(code)).ToList();
    options.DefaultRequestCulture = new RequestCulture(Languages.Default);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    // The language is defined only by the URL (/tr/..., /en/...) so every language has its own indexable address.
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new RouteDataRequestCultureProvider
        {
            RouteDataStringKey = RoutingExtensions.CultureRouteKey,
            UIRouteDataStringKey = RoutingExtensions.CultureRouteKey
        }
    };
});
        
builder.Services
    .AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.FullAccess, policy => policy.RequireRole(Roles.Admin))
    .AddPolicy(Policies.UserManagement, policy => policy.RequireRole(Roles.Admin))
    .AddPolicy(Policies.ContentManagement, policy => policy.RequireRole(Roles.Admin, Roles.Editor))
    .AddPolicy(Policies.Staff, policy => policy.RequireRole(Roles.All.ToArray()));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Prj.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/admin/account/login";
    options.LogoutPath = "/admin/account/logout";
    options.AccessDeniedPath = "/admin/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.Configure<Microsoft.AspNetCore.Identity.SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(5));

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "Prj.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Only anonymous POSTs are limited: public forms (contact, quote) and the admin login.
// Authenticated admin work is never throttled.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path;
        // The WhatsApp click beacon must not use up the visitor's form quota; it counts each IP once a day anyway.
        if (path.Equals("/api/whatsapp-click", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        if (path.StartsWithSegments("/admin/account/login", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"{RateLimitPolicies.Login}:{ip}",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 });
        }

        // Status code pages re-execute failed POSTs on /error/*; they must not consume the quota twice.
        if (path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/error", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter(string.Empty);
        }

        return RateLimitPartition.GetFixedWindowLimiter($"{RateLimitPolicies.Forms}:{ip}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 });
    });
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml", "application/xml" });
});

builder.Services.AddOutputCache();

// Emit Turkish characters as-is instead of numeric entities (smaller, more readable HTML).
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(options =>
    options.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseHttpsRedirection();
// Not in Development: Visual Studio Browser Link appends its script as plain text to the already-compressed body,
// which breaks some pages with ERR_CONTENT_DECODING_FAILED.
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}
app.UseSecurityHeaders(app.Environment.IsDevelopment());
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var headers = context.Context.Response.Headers;
        var isVersioned = context.Context.Request.Query.ContainsKey("v");
        var isUpload = context.Context.Request.Path.StartsWithSegments("/uploads");
        headers.CacheControl = isVersioned || isUpload ? "public,max-age=31536000,immutable" : "public,max-age=604800";
        if (isUpload)
        {
            headers.ContentDisposition = context.File.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? "inline" : headers.ContentDisposition;
        }
    }
});

app.UseMiddleware<LegacyRedirectMiddleware>();
app.UseRouting();
app.UseRequestLocalization();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllers();
app.MapLocalizedRoutes();
// For uptime monitors (e.g. UptimeRobot); a body is returned so status code pages do not replace the 503.
app.MapGet("/healthz", async (ApplicationDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Text("ok") : Results.Text("db unreachable", statusCode: 503));

if (app.Configuration.GetValue("Database:SeedOnStartup", true))
{
    await DbSeeder.SeedAsync(app.Services);
}

app.Run();
