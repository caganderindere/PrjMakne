# Doğrulama sonuçları — 2026-11-24

Ortam: yerel makine, `ASPNETCORE_ENVIRONMENT=Production`, seed içeriğiyle doldurulmuş boş veritabanı (`PrjE2E`).

## Lighthouse 13.6.0 (mobil, varsayılan kısıtlama)

| Sayfa | Performans | Erişilebilirlik | En İyi Uygulamalar | SEO |
|---|---|---|---|---|
| `/tr` | 98 | 100 | 100 | 100 |
| `/en` | 99 | 100 | 100 | 100 |
| `/tr/urunler/kategori/boya-pisirme-firinlari` | 100 | 100 | 100 | 100 |
| `/en/products/category/curing-ovens` | 100 | 100 | 100 | 100 |
| `/tr/urunler/manuel-corona-cihaz-seti` | 93 | 100 | 100 | 100 |
| `/en/products/manual-corona-powder-gun-set` | 96 | 100 | 100 | 100 |

Ham raporlar: `lh-*.json` (Lighthouse Viewer ile açılabilir).

## Uçtan uca testler (Playwright, `tests/e2e`)

35 test, 35 başarılı: her sayfa türü TR/EN'de 200, tek `<h1>`, benzersiz `<title>`, doğru canonical ve hreflang,
geçerli JSON-LD; 20 eski URL tek adımda 301 → 200; ürün sayfasından teklif formu ürün adıyla dolu açılıyor;
WhatsApp bağlantısı ürün adını içeriyor; varyant listesi; 390 px genişlikte yatay kaydırma yok;
yazı yokken Blog menüde ve sitemap'te yok.

Çalıştırma: `cd tests/e2e && npm install && npx playwright install chromium && npx playwright test`
