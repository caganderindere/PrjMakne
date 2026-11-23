# PRJ Proje Makina Kurumsal Web Sitesi — Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** PRJ Proje Makina A.Ş. için asprubberT altyapısından türetilmiş, TR varsayılan + tam EN, SEO eksiksiz, panelden yönetilen kurumsal site ve uçtan uca kurumsal kimlik.

**Architecture:** asprubberT'nin kodu (Domain / Application / Infrastructure / Web / Areas/Admin) PrjProje'ye **kopyalanır**, isim alanı `PrjProje` yapılır, PRJ'de kullanılmayacak modüller silinir, ardından rotalar, ürün modeli, SEO, yönlendirmeler, tasarım ve içerik PRJ'ye göre kurulur. İçerik, tek seferlik bir taşıma aracıyla eski WordPress'ten JSON seed'e alınır, metinler yeniden yazılır.

**Tech Stack:** .NET 10 (LTS), ASP.NET Core MVC, EF Core 10 + SQL Server, ASP.NET Identity, SkiaSharp (WebP), HtmlSanitizer, xUnit, Playwright (doğrulama), self-hosted Barlow / Barlow Condensed / Inter.

**Spec:** `docs/superpowers/specs/2026-11-23-prj-corporate-website-design.md`

## Global Constraints

- **asprubberT yalnızca okunur.** `C:\Users\çağan\source\repos\asprubber*` altındaki hiçbir dosya değiştirilmez, oraya komut çalıştırılmaz (build dahil).
- PRJ kodunda, görsellerinde, ayarlarında hiçbir "asprubber" / "AspRubber" / "ASPRUBBER" izi kalmaz (Task 1'deki sızıntı testi bunu her build'de zorlar).
- Hedef çatı `net10.0`; tüm `Microsoft.*` paketleri 10.0.x.
- Ana marka adı: `PRJ Proje Makina A.Ş.`; "Prestij Teknik Makine" yalnızca SEO metinlerinde eski ad olarak.
- Diller: `tr` (varsayılan, `/` → `/tr`) ve `en`. Arapça yok.
- Renkler: logo kırmızısı `#FF0000` (yalnızca logo dosyalarında), arayüz PRJ Kırmızı `#E30613`, hover `#B8050F`, Antrasit `#1C2127`, Toz Krem vurgu (J'deki krem; logodan örneklenir).
- Fontlar: başlık Barlow / Barlow Condensed, metin Inter; `wwwroot/fonts` altından, `latin-ext` alt kümesi.
- Kaynakta olmayan teknik değer yazılmaz; boş tablo gizlenir.
- İçeriği olmayan menü/bölüm gizlenir (örn. yazısız blog).
- Gizli bilgiler (WP şifresi, SMTP, bağlantı dizesi) repoya yazılmaz; `dotnet user-secrets` veya ortam değişkeni.
- Roller: `Admin`, `Editor`.

## Review Focus

1. **Türkçe karakterli slug'lar** (`ç ğ ı İ ö ş ü`, özellikle `İ`'nin küçük harfe dönüşümü) → ASCII slug: "Özel İmalat Ürünler" → `ozel-imalat-urunler`. Test: Task 3.
2. **Eski URL varyantları** (sonda `/` olan/olmayan, büyük harf, yüzde kodlu Türkçe, `?lang=en`, `/en/...` GTranslate yolu) → hepsi tek bir 301 hedefine, zincirsiz. Test: Task 5.
3. **Bir dili eksik ürün** (EN çevirisi henüz girilmemiş) → o dilde 404 değil, hreflang'de o dil hiç yazılmaz, sitemap'te yalnızca mevcut dil yer alır. Test: Task 6.
4. **Teklif formuna büyük/yanlış dosya** (25 MB, `.exe`, uzantısı `.pdf` olan çalıştırılabilir) → kibar doğrulama hatası, dosya kaydedilmez. İzinli: pdf, jpg, jpeg, png, dwg, dxf, step, stp, zip; en fazla 10 MB. Test: Task 8.
5. **Panelden dev/CMYK/bozuk görsel yükleme** → WebP'ye çevrilir veya kibar hata; uygulama çökmez, en uzun kenar 2000 px'e indirilir. Test: Task 9.

---

## Dosya yapısı (hedef)

```
PrjProje/                         (repo kökü, git init)
  PrjProje.sln
  PrjProje/                        (web projesi; boş iskelet asprubberT kopyasıyla değiştirilir)
    Domain/ Application/ Infrastructure/ Web/ Areas/Admin/ Views/ Resources/ wwwroot/
  PrjProje.Tests/
  tools/PrjMigrate/                (tek seferlik WP → JSON taşıma konsolu)
  content/seed/                    (categories.json, products.json, services.json, pages.json, redirects.json)
  content/images/                  (taşınmış orijinal görseller)
  docs/brand/                      (logo/, brand-guide.html, brand-guide.pdf, templates/)
  docs/superpowers/{specs,plans}/
```

---

### Task 1: Repo, kopya, yeniden adlandırma, .NET 10 ve sızıntı testi

**Files:**
- Create: `.gitignore` (asprubberT'ninkine ek olarak `content/images/raw/`, `**/appsettings.Production.json`)
- Replace: `PrjProje/` içeriği ← `asprubberT/asprubberT/` (hariç: `bin obj .vs App_Data wwwroot/uploads wwwroot/images Properties/PublishProfiles Infrastructure/Data/Migrations *.csproj.user appsettings.Development.json CLAUDE.md`)
- Create: `PrjProje.Tests/` ← `asprubberT.Tests/` (yalnızca proje dosyası ve `ProductSearchTests.cs`, `RolesTests.cs`)
- Test: `PrjProje.Tests/BrandLeakTests.cs`

**Interfaces:**
- Produces: kök isim alanı `PrjProje` (`PrjProje.Domain.Entities`, `PrjProje.Web.Routing`, …), assembly adı `PrjProje`, cookie adları `Prj.Auth`, `Prj.Antiforgery`, yeni `UserSecretsId` `prj-web-<yeni-guid>`.

- [ ] **Step 1:** `git init`, mevcut iskeleti (`Controllers/HomeController.cs`, `Models/`, `Views/Home`, `wwwroot/lib`) sil, `wwwroot/image/logo.png` → `docs/brand/source/logo.png` taşı. Kopyayı `robocopy` ile al (kaynak salt okunur).
- [ ] **Step 2: Failing test** `BrandLeakTests.No_source_file_mentions_the_reference_project`: repo kökünden `PrjProje/`, `PrjProje.Tests/`, `tools/`, `content/` altındaki tüm metin dosyalarını (`.cs .cshtml .css .js .json .resx .xml .config .csproj .webmanifest .txt`) tarar; büyük/küçük harf duyarsız `asprubber` geçen dosya listesini `Assert.Empty` ile doğrular. (Test dosyasının kendisi hariç.)
- [ ] **Step 3:** `dotnet test` → FAIL (çok sayıda dosya listelenir).
- [ ] **Step 4:** `AspRubber`→`PrjProje`, `asprubberT.Tests`→`PrjProje.Tests` isim alanı değişimi; tüm marka metinleri, e-posta/telefon/adres, GA/Cloudflare/SMTP ayarları, `.resx` içindeki Asprubber metinleri, `site.webmanifest` temizlenir (metinler geçici olarak nötr "PRJ Proje Makina" yazılır). `TargetFramework` `net10.0`, paketler 10.0.x. `ProductSearchTests` örnek verisi PRJ ürünlerine çevrilir (örn. "B 400 Manuel Corona Cihazı", "Siklon Otomatik Kabin").
- [ ] **Step 5:** `dotnet build` ve `dotnet test` → PASS (sızıntı testi dahil).
- [ ] **Step 6: Commit** — `docs/` (spec + plan) dahil: `chore: bootstrap PrjProje from reference architecture on .NET 10`.

### Task 2: Kullanılmayacak modüllerin silinmesi ve rollerin sadeleşmesi

**Files:**
- Delete: Defense, Fair, Card/BusinessCard, Meeting, Developer/DeveloperTickets, Careers/JobApplication, Cloudflare, Analytics(Looker), ExportRegions, CareerAreas, CardLinks, 3D (`hero-3d.*`, `defense-3d.*`, `three-studio.js`, `intro.js`), `report.*`, `card.css` ile bunlara ait controller, view, servis, arayüz, model, entity, DbSet, konfigürasyon ve admin menü öğeleri.
- Modify: `Application/Common/Roles.cs` (veya rollerin tanımlandığı dosya), `Program.cs` politikaları.
- Create: `PrjProje/Infrastructure/Data/Migrations/*_InitialCreate.cs`
- Test: `PrjProje.Tests/RolesTests.cs`

**Interfaces:**
- Produces: `Roles.Admin = "Admin"`, `Roles.Editor = "Editor"`, `Roles.All`; politikalar `Policies.FullAccess` (Admin), `Policies.UserManagement` (Admin), `Policies.ContentManagement` (Admin, Editor), `Policies.Staff` (All).

- [ ] **Step 1: Failing test** `RolesTests.Only_admin_and_editor_exist`: `Assert.Equal(["Admin", "Editor"], Roles.All)`.
- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Modülleri sil, rolleri ve politikaları sadeleştir; `QuoteRequest`, `ContactMessage`, `WhatsAppClickDay`, `Document`, `Gallery`, `BlogPost`, `Capability`, `Product*`, `SiteSetting`, `SiteContent`, `ActivityLog`, `ApplicationUser` kalır. `dotnet ef migrations add InitialCreate`.
- [ ] **Step 4:** `dotnet build` (uyarısız sayıda yeni hata yok) ve `dotnet test` → PASS. `dotnet run` → `/tr` 200 döner (LocalDB).
- [ ] **Step 5: Commit** `refactor: remove modules PRJ does not use`.

### Task 3: PRJ rotaları, Hizmetler (Capability → Service) ve slug kuralları

**Files:**
- Modify: `PrjProje/Web/Routing/SiteRoutes.cs`, `PrjProje/Application/Common/SlugHelper.cs`
- Rename: `Capability*` → `Service*` (entity, translation, servis, admin controller/views, public controller `ServicesController`, view klasörü `Views/Services`); `Quality` → `Certificates`; `Catalog` → `Manuals`; `Gallery` → `Videos` (galeri + YouTube videoları aynı sayfada).
- Test: `PrjProje.Tests/SiteRoutesTests.cs`, `PrjProje.Tests/SlugHelperTests.cs`

**Interfaces:**
- Produces: route anahtarları `Home, About, Certificates, Products, ProductCategory, ProductDetail, Services, ServiceDetail, Videos, Manuals, Blog, BlogDetail, Quote, Contact, Privacy, Cookies`; `SiteRoutes.Path(string key, string lang, string? slug = null)`; `SlugHelper.Generate(string text) -> string`.

- [ ] **Step 1: Failing tests** `SiteRoutesTests.Paths_match_spec` — `[Theory]` ile spec §6 tablosundaki her satır, örn.:
  `("About","tr",null,"/tr/kurumsal")`, `("About","en",null,"/en/about-us")`, `("Certificates","en",null,"/en/certificates")`, `("ProductCategory","tr","toz-boya-cihazlari","/tr/urunler/kategori/toz-boya-cihazlari")`, `("ProductDetail","en","manual-corona-gun-set","/en/products/manual-corona-gun-set")`, `("ServiceDetail","tr","x","/tr/hizmetler/x")`, `("Videos","tr",null,"/tr/videolar")`, `("Manuals","tr",null,"/tr/kullanim-kilavuzlari")`, `("Manuals","en",null,"/en/user-manuals")`, `("Quote","en",null,"/en/request-a-quote")`, `("Privacy","tr",null,"/tr/kvkk")`, `("Home","tr",null,"/tr")`.
  `SlugHelperTests`: `"Özel İmalat Ürünler"→"ozel-imalat-urunler"`, `"Kimyasal Yüzey Temizlik Hatları"→"kimyasal-yuzey-temizlik-hatlari"`, `"B 400 Manuel Cihaz"→"b-400-manuel-cihaz"`, `"  Şişe -- Çığ  "→"sise-cig"`, `"İ"`→`"i"` (kültürden bağımsız).
- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Rota tablosunu ve yeniden adlandırmaları uygula; `SlugHelper` Türkçe eşlemeyi `ToLowerInvariant` öncesi elle yapar. Migration `RenameCapabilityToService`.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5: Commit** `feat: PRJ site routes, services module and Turkish-safe slugs`.

### Task 4: Ürün modeli — model/varyant listesi

**Files:**
- Modify: `Domain/Entities/Product.cs` (`OemNumber`, `ServiceCode` kaldır; `ProductTranslation.Material`, `ProductionMethod` kaldır), `Infrastructure/Services/ProductSearch.cs`, ürün admin formu ve detay view'ı.
- Create: `ProductVariant` entity, migration `ProductVariants`.
- Test: `PrjProje.Tests/ProductSearchTests.cs`

**Interfaces:**
- Produces: `ProductVariant { int Id; int ProductId; string Code; int SortOrder; }` (dil bağımsız, örn. "B 300", "YB 500"); `Product.Variants`; `ProductSearch.Document(string Name, string Category, string? Text, string? VariantCodes)`.

- [ ] **Step 1: Failing tests:** `Variant_codes_match_in_any_format` — `"yb500"`, `"YB-500"`, `"YB 500"` sorguları "Manuel Corona Cihaz Seti"ni ilk sırada döndürür; `Name_search_ignores_turkish_case` — `"SİKLON"` "Siklon Otomatik Kabin"i bulur.
- [ ] **Step 2:** FAIL. **Step 3:** Uygula (varyant kodları eski `ServiceCode` mantığıyla normalize edilir). Admin formunda sıralanabilir varyant listesi. Ürün detayında varyantlar rozet listesi olarak, özellik tablosu yalnızca satır varsa.
- [ ] **Step 4:** PASS. **Step 5: Commit** `feat: product variants replace OEM/service codes`.

### Task 5: Eski URL'ler için 301 yönlendirme

**Files:**
- Create: `Domain/Entities/LegacyRedirect.cs`, `Web/Infrastructure/LegacyRedirectMiddleware.cs`, `Application/Common/LegacyPath.cs`, admin `RedirectsController` + views, migration `LegacyRedirects`.
- Test: `PrjProje.Tests/LegacyPathTests.cs`, `PrjProje.Tests/LegacyRedirectMiddlewareTests.cs`

**Interfaces:**
- Produces: `LegacyRedirect { int Id; string FromPath; string ToPath; int HitCount; }` (FromPath benzersiz, normalize hâlde); `LegacyPath.Normalize(string pathAndQuery) -> (string Path, string? Lang)`; middleware routing'den önce, eşleşirse `301` + `Location: ToPath` (EN için ToPath EN karşılığı).

- [ ] **Step 1: Failing tests:** `Normalize`: `"/urunler/Manuel-Corona-Cihaz-Seti/"`→`("/urunler/manuel-corona-cihaz-seti",null)`; `"/urunler/manuel-corona-cihaz-seti"` aynı; `"/urun-kategori/toz-boya-cihazlar%C4%B1/"`→ çözülmüş ve küçük harfli; `"/kurumsal/?lang=en"`→`("/kurumsal","en")`; `"/en/kurumsal/"`→`("/kurumsal","en")`. Middleware: kayıtlı `/kurumsal` → `/tr/kurumsal`, `lang=en` ile `/en/about-us` (tablodaki EN hedef sütunu); kayıtsız eski yol → `next` çağrılır; `/tr/...` ve `/admin/...` yolları hiç tablo sorgusu yapmaz; zincir yok (hedef her zaman yeni sitenin yolu).
- [ ] **Step 2:** FAIL. **Step 3:** Uygula; tablo `ContentCache` ile bellekte tutulur, HitCount arka planda artar. `LegacyRedirect`'e `ToPathEn` alanı ekle (EN hedefi; boşsa TR hedefi). Admin'de listele/ekle/düzenle/sil + hit sayısı.
- [ ] **Step 4:** PASS. **Step 5: Commit** `feat: legacy WordPress URL 301 redirects`.

### Task 6: SEO — yapılandırılmış veri, hreflang, sitemap, meta yedekleri

**Files:**
- Create: `Web/Seo/JsonLd.cs`
- Modify: `Web/Seo/PageSeo.cs`, `Views/Shared/_SeoHead.cshtml`, sitemap/robots üreten `SystemControllers.cs`.
- Test: `PrjProje.Tests/JsonLdTests.cs`, `PrjProje.Tests/SeoAlternatesTests.cs`

**Interfaces:**
- Produces: `JsonLd.Organization(SiteSettingsDto s, string baseUrl) -> string`, `JsonLd.Product(ProductDetailViewModel p, string url) -> string`, `JsonLd.Breadcrumb(IEnumerable<(string Name, string Url)>) -> string`, `JsonLd.Video(string name, string youtubeId, string description) -> string`; `PageSeo.Alternates` yalnızca çevirisi olan diller + `x-default` (TR); `PageSeo.FallbackDescription(string text) -> string` (HTML'siz, ≤160 karakter, kelime sınırında).

- [ ] **Step 1: Failing tests:** Organization JSON'unda `"@type":["Organization","LocalBusiness"]`, `"name":"PRJ Proje Makina A.Ş."`, `"alternateName":"Prestij Teknik Makine"`, `addressLocality "Arnavutköy"`, `postalCode "34281"`, telefon `+902125440104`, `sameAs` üç sosyal hesap. Product JSON'unda `"brand":{"@type":"Brand","name":"PRJ"}` var, `offers` **yok**. Alternates: yalnız TR çevirisi olan ürün için `[tr, x-default]`. `FallbackDescription` 300 karakterlik metni ≤160'a, kelime ortasından bölmeden keser. Arama sonuç sayfasında `robots` `noindex,follow`.
- [ ] **Step 2:** FAIL. **Step 3:** Uygula (System.Text.Json, elle string birleştirme yok). Sitemap her dil için yalnızca mevcut çevirileri, `xhtml:link` alternatifleriyle listeler.
- [ ] **Step 4:** PASS. **Step 5: Commit** `feat: structured data, hreflang alternates and meta fallbacks`.

### Task 7: Kurumsal kimlik — logo seti, token'lar, fontlar, marka rehberi

**Files:**
- Create: `docs/brand/logo/prj-logo.svg`, `prj-logo-horizontal.svg`, `prj-monogram.svg`, `prj-logo-negative.svg`, `prj-logo-mono-black.svg`; `PrjProje/wwwroot/images/brand/` (aynı SVG'ler + `og-default.jpg` 1200×630); favicon seti (`favicon.ico`, `favicon.svg`, 96, 180 apple-touch, 192, 512) ve `site.webmanifest`; `PrjProje/wwwroot/fonts/` (Barlow 500/600/700, Barlow Condensed 600/700, Inter 400/500/600; woff2, latin-ext); `PrjProje/wwwroot/css/tokens.css`; `docs/brand/brand-guide.html` → `docs/brand/brand-guide.pdf`; `docs/brand/templates/` (antetli kağıt A4, kartvizit 85×55, e-posta imzası HTML, sosyal medya 1080×1080 şablonu).
- Test: `PrjProje.Tests/BrandAssetsTests.cs`

**Interfaces:**
- Produces: CSS değişkenleri `--prj-red:#E30613; --prj-red-hover:#B8050F; --prj-logo-red:#FF0000; --prj-anthracite:#1C2127; --prj-cream:<örneklenen>; --prj-steel-50…900; --font-display:"Barlow Condensed"; --font-heading:"Barlow"; --font-body:"Inter"; --radius-*; --space-*; --shadow-*`.

- [ ] **Step 1: Failing tests:** `tokens.css` içinde yukarıdaki değişkenler tanımlı; `--prj-red` ile beyaz arasındaki kontrast oranı ≥ 4.5 (test içinde WCAG göreli parlaklık hesabı); tüm logo SVG'leri geçerli XML, `viewBox` var, `<title>` "PRJ Proje Makina A.Ş."; manifest'teki her ikon dosyası mevcut.
- [ ] **Step 2:** FAIL. **Step 3:** SVG'leri `docs/brand/source/logo.png`'den birebir yeniden çiz (P, R, J geometrisi; J ucundaki krem dikdörtgen; alt yazı serif "PROJE MAKİNA A.Ş"). Krem rengi PNG'den örnekle. Marka rehberi: logo, koruma alanı, minimum boyut, yanlış kullanımlar, renkler (HEX/RGB/CMYK), tipografi hiyerarşisi, görsel dil, şablon örnekleri; Playwright ile PDF'e bas.
- [ ] **Step 4:** PASS + PNG'ye render edilen logo ile orijinali yan yana ekran görüntüsünde karşılaştır (göz kontrolü, kullanıcıya gösterilir).
- [ ] **Step 5: Commit** `feat: PRJ brand identity, logo set, tokens and brand guide`.

### Task 8: Genel düzen ve dönüşüm öğeleri (header, footer, WhatsApp, mobil bar, teklif formu)

**Files:**
- Modify: `Views/Shared/_Layout.cshtml`, `_LanguageMenu.cshtml`, `_CtaSection.cshtml`, `Views/Quote/Index.cshtml`, `Views/Contact/Index.cshtml`, `Web/Controllers/FormControllers.cs`, `Application/Models/FormModels.cs`; `wwwroot/css/site.css` (baştan, `tokens.css` tabanlı), `wwwroot/js/site.js`.
- Create: `Views/Shared/_MobileActionBar.cshtml`, `Views/Shared/_MegaMenu.cshtml`, `Application/Common/QuoteAttachmentRules.cs`.
- Test: `PrjProje.Tests/QuoteAttachmentRulesTests.cs`, `PrjProje.Tests/WhatsAppLinkTests.cs`

**Interfaces:**
- Produces: `QuoteAttachmentRules.Validate(string fileName, long length, ReadOnlySpan<byte> header) -> string? error` (null = geçerli); `WhatsAppLink.Build(string phone, string message) -> string` (`https://wa.me/90…?text=…`); teklif sayfası `?product={id}` ile ürün adını önceden doldurur.

- [ ] **Step 1: Failing tests:** izinli uzantılar spec'teki liste, 10 MB sınırı, `.pdf` uzantılı ama `MZ` başlıklı dosya reddedilir, `%PDF` başlıklı kabul; `WhatsAppLink.Build("0 531 796 00 29","B 400 hakkında bilgi")` → `https://wa.me/905317960029?text=B%20400%20hakk%C4%B1nda%20bilgi`.
- [ ] **Step 2:** FAIL. **Step 3:** Uygula. Header: logo, mega menü (9 kategori, görselli), Hizmetler, Kurumsal (Kurumsal + Belgeler), Videolar, Kılavuzlar, İletişim, dil menüsü, telefon, "Teklif Al" butonu; scroll'da küçülür. Footer: iletişim, kategoriler, sosyal hesaplar, KVKK/Çerez. Sağ altta WhatsApp (masaüstü); mobilde alt bar Ara (`tel:+902125440104`) · WhatsApp · Teklif. Tüm metinler `.resx` (TR/EN).
- [ ] **Step 4:** PASS + Playwright: 390px ve 1440px'te `/tr` ve `/en` ekran görüntüsü, yatay kaydırma yok, teklif formu gönderimi `QuoteRequest` kaydı oluşturur.
- [ ] **Step 5: Commit** `feat: corporate layout and conversion paths`.

### Task 9: Panel uyarlaması ve görsel işleme

**Files:**
- Modify: `Areas/Admin/**` (PRJ logosu/renkleri, menü: Dashboard, Ürünler, Kategoriler, Hizmetler, Belgeler & Kılavuzlar, Galeri & Videolar, Referanslar, Blog, Site Metinleri, Teklifler, Mesajlar, Yönlendirmeler, Ayarlar, Kullanıcılar), `Infrastructure/Services/Support/FileStorageService.cs`, `Application/Models/SiteSettingsDto.cs`.
- Create: `Infrastructure/Services/Support/ImageProcessor.cs`; `ProductImage.AltTr`, `ProductImage.AltEn` + migration `ImageAltText`.
- Test: `PrjProje.Tests/ImageProcessorTests.cs`

**Interfaces:**
- Produces: `ImageProcessor.ToWebp(Stream input, int maxEdge = 2000, int quality = 82) -> Result<byte[]>`; ayar anahtarları `Contact.Phone, Contact.Fax, Contact.Gsm, Contact.Email, Contact.SalesEmail, Contact.Address, WhatsApp.Number, WhatsApp.MessageTr, WhatsApp.MessageEn, Social.Instagram, Social.Facebook, Social.YouTube, Analytics.Ga4Id, Smtp.*`.

- [ ] **Step 1: Failing tests:** 4000×3000 PNG → en uzun kenar 2000 WebP (RIFF/WEBP başlığı); bozuk bayt dizisi → `Result` hata, istisna fırlatmaz; şeffaf PNG şeffaflığı korur. Ürün görseli kaydı TR alt metni boşsa doğrulama hatası.
- [ ] **Step 2:** FAIL. **Step 3:** Uygula; yükleme sırasında `srcset` için 480/960/1600 genişlik türevleri üretilir.
- [ ] **Step 4:** PASS + panelde ürün oluştur/düzenle/görsel yükle akışı Playwright ile.
- [ ] **Step 5: Commit** `feat: PRJ admin panel, WebP pipeline and required alt text`.

### Task 10: Taşıma aracı (WordPress → JSON seed)

**Files:**
- Create: `tools/PrjMigrate/PrjMigrate.csproj` (konsol, net10.0), `WpClient.cs`, `Mapper.cs`, `Program.cs`; `PrjProje.Tests/Migrate/MapperTests.cs` + `PrjProje.Tests/Migrate/fixtures/*.json`.
- Output: `content/seed/categories.json`, `products.json`, `pages.json`, `redirects.json`, `content/images/raw/` (git dışı) ve `content/images/products/` (seçilmiş orijinaller).

**Interfaces:**
- Consumes: `PRJ_WP_USER`, `PRJ_WP_APP_PASSWORD` ortam değişkenleri (WordPress Uygulama Şifresi; asla dosyaya yazılmaz).
- Produces: seed şeması `SeedProduct { string LegacySlug; string CategoryLegacySlug; string NameTr; string? NameEn; string[] Variants; string[] Images; string LegacyHtmlTr; Dictionary<string,string> SpecsTr; }`, `SeedRedirect { string From; string ToTr; string? ToEn; }`.

- [ ] **Step 1: Failing tests:** fixture'daki WooCommerce ürün JSON'u → `SeedProduct` (varyantlar "B 300…YB 500" açıklamadaki listeden çıkarılır; görseller `-600x640`, `-300x300_c` küçük resimleri atılıp orijinal URL'ye indirgenir); `wp-sitemap` fixture'ından tüm eski yollar `SeedRedirect` üretir, her biri yeni rotaya eşlenir (eşlenemeyen olursa test listeler ve FAIL).
- [ ] **Step 2:** FAIL. **Step 3:** Uygula: `/wp-json/wc/v3/products?per_page=100`, `/wp-json/wc/v3/products/categories`, `/wp-json/wp/v2/pages`, `/wp-sitemap.xml`; görselleri orijinal çözünürlükte indir.
- [ ] **Step 4:** PASS; gerçek çalıştırma → kategori sayısı 9, ürün sayısı ve görsel sayısı raporlanır, eşlenemeyen eski URL 0.
- [ ] **Step 5: Commit** (görsel ham klasörü hariç) `feat: WordPress content migration tool and seed export`.

### Task 11: İçerik — TR/EN metinler, seed yükleme, yasal sayfalar

**Files:**
- Modify: `content/seed/*.json` (yeni metinler), `Infrastructure/Data/DbSeeder.cs` (seed JSON'dan idempotent yükleme), `Resources/SharedResource.tr.resx`, `.en.resx`, `Views/Legal/*`.
- Test: `PrjProje.Tests/SeedContentTests.cs`

**Interfaces:**
- Consumes: Task 10 seed şeması; Task 3 `SlugHelper.Generate`; Task 5 `LegacyRedirect`.

- [ ] **Step 1: Failing tests** (seed JSON üzerinde): her aktif ürün ve kategori için TR **ve** EN `Name`, `ShortDescription`, `Description`, `MetaTitle` (≤ 60), `MetaDescription` (70–160) dolu; slug'lar dil içinde benzersiz; iki ürünün `Description`'ı aynı değil ve ortak 12-kelimelik dizi içermiyor (kopya metin önlemi); `SpecsTr` dışında sayısal teknik değer içeren cümle yok mu diye kaynak HTML ile çapraz kontrol (sayı + birim deseni, kaynakta olmayan → FAIL listesi); her eski URL için redirect kaydı var.
- [ ] **Step 2:** FAIL. **Step 3:** Metinleri yaz: kurumsal (misyon/vizyon kaynaktan), hizmet detayları, 9 kategori girişi, her ürün; ardından EN profesyonel karşılıklar (sektör terimleri: corona/tribo powder gun, powder coating booth, curing oven, cyclone recovery, pretreatment line, conveyor). KVKK, gizlilik, çerez şablonları firma bilgileriyle doldurulur; yayından önce hukuki onay gerektiği kullanıcıya ayrıca bildirilir. Seeder görselleri `ImageProcessor` ile WebP'ye çevirip yükler.
- [ ] **Step 4:** PASS; temiz veritabanında `dotnet run` → tüm ürünler TR/EN görünür.
- [ ] **Step 5: Commit** `content: original TR/EN copy for all categories, products and pages`.

### Task 12: Sayfa tasarımları, uçtan uca doğrulama, yayın profili

**Files:**
- Modify: `Views/Home/Index.cshtml` (spec §6 akışı), `Views/Products/{Index,Detail}.cshtml`, `Views/Services/*`, `Views/About/Index.cshtml`, `Views/Certificates/*`, `Views/Videos/*`, `Views/Manuals/*`, `Views/Blog/*`, `Views/Error/Index.cshtml`.
- Create: `PrjProje/Properties/PublishProfiles/PrjFolder.pubxml` (Folder publish, `net10.0`, `win-x64`, framework-dependent; sırsız), `docs/deploy.md` (IIS + .NET 10 Hosting Bundle + SQL + user-secrets/ortam değişkenleri + DNS geçişi + Search Console'a sitemap bildirimi), `tests/e2e/` Playwright betikleri.

- [ ] **Step 1: Failing e2e:** her sayfa türü TR ve EN'de 200, tek `<h1>`, `<title>` benzersiz, hreflang/canonical doğru, JSON-LD parse edilir; 20 rastgele eski URL tek adımda 301 → 200; ürün sayfasından "Teklif Al" formu ürün adıyla dolu açılır; WhatsApp bağlantısı ürün adını içerir; 390px'te yatay kaydırma yok; yayında yazı yokken Blog menüde görünmez ve `/tr/blog` sitemap'te yer almaz.
- [ ] **Step 2:** FAIL. **Step 3:** Sayfaları tasarla (spec §6), ölçülü animasyonlar `prefers-reduced-motion` ile kapanır.
- [ ] **Step 4:** e2e PASS; Lighthouse mobil: SEO 100, Erişilebilirlik 100, En İyi Uygulamalar 100, Performans ≥ 90 (ana sayfa, kategori, ürün detay; TR ve EN). Sonuçlar `docs/verification/` altına.
- [ ] **Step 5: Commit** `feat: page designs, e2e verification and IIS publish profile`.
