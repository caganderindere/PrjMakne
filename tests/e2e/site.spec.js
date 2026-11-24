const { test, expect } = require("@playwright/test");
const redirects = require("../../content/seed/redirects.json");
const catalog = require("../../content/seed/catalog.json");

const product = catalog.Products.find((p) => p.LegacySlug === "manuel-corona-cihaz-seti");
const category = catalog.Categories[0];
const service = catalog.Services[0];

// One page of every type in both languages.
const pages = [
    ["/tr", "/en"],
    ["/tr/kurumsal", "/en/about-us"],
    ["/tr/belgeler", "/en/certificates"],
    ["/tr/urunler", "/en/products"],
    [`/tr/urunler/kategori/${category.Tr.Slug}`, `/en/products/category/${category.En.Slug}`],
    [`/tr/urunler/${product.Tr.Slug}`, `/en/products/${product.En.Slug}`],
    ["/tr/hizmetler", "/en/services"],
    [`/tr/hizmetler/${service.Tr.Slug}`, `/en/services/${service.En.Slug}`],
    ["/tr/videolar", "/en/videos"],
    ["/tr/kullanim-kilavuzlari", "/en/user-manuals"],
    ["/tr/teklif-al", "/en/request-a-quote"],
    ["/tr/iletisim", "/en/contact"],
    ["/tr/blog", "/en/blog"],
    ["/tr/kvkk", "/en/privacy-policy"],
    ["/tr/cerez-politikasi", "/en/cookie-policy"],
];

for (const pair of pages) {
    for (const [index, path] of pair.entries()) {
        test(`page ${path}`, async ({ page, request }) => {
            const response = await request.get(path, { maxRedirects: 0 });
            expect(response.status()).toBe(200);

            await page.goto(path);
            await expect(page.locator("h1")).toHaveCount(1);

            expect((await page.title()).length).toBeGreaterThan(0);

            const canonical = await page.locator('link[rel="canonical"]').getAttribute("href");
            expect(new URL(canonical).pathname).toBe(path);

            const hreflang = await page.locator('link[rel="alternate"][hreflang]').evaluateAll((links) =>
                Object.fromEntries(links.map((l) => [l.hreflang, new URL(l.href).pathname])));
            expect(hreflang.tr).toBe(pair[0]);
            expect(hreflang.en).toBe(pair[1]);
            expect(hreflang["x-default"]).toBeTruthy();

            for (const json of await page.locator('script[type="application/ld+json"]').allTextContents()) {
                expect(() => JSON.parse(json)).not.toThrow();
            }
            expect(index).toBeLessThan(2);
        });
    }
}

test("every page has its own <title>", async ({ request }) => {
    const seen = new Map();
    for (const path of pages.flat()) {
        const html = await (await request.get(path)).text();
        const title = /<title>([^<]*)<\/title>/.exec(html)[1];
        expect(seen.get(title) ?? path, `"${title}" is used by ${seen.get(title)} and ${path}`).toBe(path);
        seen.set(title, path);
    }
});

test("20 legacy URLs redirect once with 301 to a live page", async ({ request }) => {
    const sample = [...redirects].sort((a, b) => (a.From > b.From ? 1 : -1)).filter((_, i, all) => i % Math.ceil(all.length / 20) === 0).slice(0, 20);
    for (const entry of sample) {
        const first = await request.get(`${entry.From}/`, { maxRedirects: 0 });
        expect(first.status(), entry.From).toBe(301);
        const target = new URL(first.headers()["location"], "http://localhost").pathname;
        const second = await request.get(target, { maxRedirects: 0 });
        expect(second.status(), `${entry.From} -> ${target}`).toBe(200);
    }
});

test("product page opens the quote form with the product filled in", async ({ page }) => {
    await page.goto(`/tr/urunler/${product.Tr.Slug}`);
    await page.getByRole("link", { name: /teklif/i }).first().click();
    await expect(page).toHaveURL(/teklif-al\?product=\d+/);
    await expect(page.locator("[data-product-input]")).toHaveValue(product.Tr.Name);
});

test("WhatsApp link carries the product name", async ({ page }) => {
    await page.goto(`/tr/urunler/${product.Tr.Slug}`);
    const href = await page.locator("a[data-whatsapp]").first().getAttribute("href");
    expect(decodeURIComponent(href)).toContain(product.Tr.Name);
});

test("product variants are listed", async ({ page }) => {
    await page.goto(`/tr/urunler/${product.Tr.Slug}`);
    for (const code of ["B 300", "B 400", "B 500", "YB 400", "YB 500"]) {
        await expect(page.locator(".variants li").getByText(code, { exact: true })).toHaveCount(1);
    }
});

test("no horizontal scrolling at 390px", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    for (const path of pages.flat()) {
        await page.goto(path);
        const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
        expect(overflow, path).toBeLessThanOrEqual(0);
    }
});

test("menu groups pages under corporate, products, services, media and contact", async ({ page, request }) => {
    await page.goto("/tr");
    const top = await page.locator(".main-nav .nav-list > li > .nav-link").allTextContents();
    expect(top.map((t) => t.trim())).toEqual(["Kurumsal", "Ürünler", "Hizmetler", "Medya", "İletişim"]);
    await expect(page.locator('#sub-about a[href="/tr/belgeler"]')).toHaveCount(1);
    await expect(page.locator('#sub-media a[href="/tr/blog"]')).toHaveCount(1);
    await expect(page.locator('#sub-media a[href="/tr/videolar"]')).toHaveCount(1);
    const sitemap = await (await request.get("/sitemap.xml")).text();
    expect(sitemap).toContain("/tr/blog");
    expect(sitemap).toContain(`/tr/urunler/${product.Tr.Slug}`);
});

test("seeded blog posts are listed and open", async ({ page }) => {
    await page.goto("/tr/blog");
    const links = page.locator(".card-title a");
    expect(await links.count()).toBeGreaterThanOrEqual(3);
    await links.first().click();
    await expect(page.locator("h1")).toHaveCount(1);
    await expect(page.locator(".prose h2").first()).toBeVisible();
});
