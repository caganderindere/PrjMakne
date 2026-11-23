# PRJ Proje Makina — Kurumsal Web Sitesi ve Kurumsal Kimlik Tasarımı

Tarih: 2026-11-23 · Durum: Onaylandı (2026-11-23)

## 1. Amaç ve neden

PRJ Proje Makina A.Ş. (prestijteknikmakine.com.tr) elektrostatik toz boya ekipmanları üretiyor: toz boya cihazları, boya kabinleri, pişirme fırınları, konveyörler, basınçlı hava, kimyasal ön işlem hatları, siklonlar ve anahtar teslim tesis kurulumu. Mevcut WordPress/WooCommerce sitesi eski görünüyor, ürün metinleri birbirinin kopyası (örneğin Manuel Corona sayfasında Tribo metni var), teknik tablo yok ve İngilizce desteği yalnızca GTranslate ile yapılan makine çevirisinden ibaret.

**Hedef:** Yurt içinden ve yurt dışından daha fazla müşteri çekmek. Ürünleri çok profesyonel bir kurumsal kimlikle vitrine koymak ve ziyaretçiyi teklif formu, WhatsApp ve telefon üzerinden bize ulaşmaya yönlendirmek.

**Başarı ölçütü:**
- Her ürün sayfasından tek tıkla teklif ya da WhatsApp mesajı gönderilebiliyor.
- TR ve EN sayfaları Google'da ayrı ayrı ve doğru dilde indeksleniyor.
- Eski adreslerin Google'daki sıralaması yeni siteye taşınıyor.
- Firma sahibi ürün, görsel ve metinleri yazılımcıya ihtiyaç duymadan panelden yönetebiliyor.

## 2. Hedefler, hedef dışı olanlar, kaçınılacaklar

**Hedefler**
- Site Türkçe açılır (`/` → `/tr`). İngilizce tam ve insan kalitesinde bir çeviri olur, `/en/...` altında kendi adresleriyle yayınlanır.
- Teknik SEO eksiksiz olur: dile özel slug, hreflang, canonical, JSON-LD, sitemap, robots, OG, hız ve 301 yönlendirmeler.
- Logodan türetilmiş, uçtan uca bir kurumsal kimlik hazırlanır ve sitede uygulanır.
- Kurumsal tasarımlı, mobil öncelikli ve dönüşüm odaklı bir UX kurulur.
- Mevcut sitedeki tüm ürünler, kategoriler ve görseller taşınır. Görseller olduğu gibi kullanılır, metinler her ürüne özel yeniden yazılır.
- Yönetim paneli kurulur.

**Hedef dışı (bu sürümde yok)**
- E-ticaret, fiyat ve sepet
- Arapça ve diğer diller. Altyapı yeni dil eklemeye hazır olacak.
- Kariyer/İK, fuar, kartvizit, savunma, 3B model, toplantı tutanakları ve geliştirici talep modülleri
- İhracat haritası (veri yok)

**Kaçınılacaklar (çalışsa bile başarısızlık sayılır)**
- **Uydurma teknik değer yazmak.** Kaynakta olmayan kapasite, ölçü ya da güç bilgisi yazılmaz. Tablo boş bırakılır ve panelden doldurulur.
- **Asprubber projesinden herhangi bir iz taşımak.** İsim, metin, görsel, ayar, bağlantı dizesi, publish profili ve secret bunun kapsamında. Asprubber ayrı bir müşteri; asprubberT yalnızca kod örneği olarak kullanılır ve **o depoya hiç dokunulmaz.**
- **Boş bölüm göstermek.** İçeriği olmayan menü ya da bölüm (örneğin yazısı olmayan blog) gizli kalır.
- **Eski URL'leri 404'e düşürmek.**

## 3. Kısıtlar

- **Altyapı:** ASP.NET Core MVC, EF Core ve SQL Server. Kodda asprubberT ile aynı katmanlar kullanılır: Domain, Application, Infrastructure, Web ve Areas/Admin.
- **Hedef çatı:** **.NET 10 (LTS)**. asprubberT .NET 8 kullanıyor, ancak .NET 8'in desteği 10 Kasım 2026'da bitti. PrjProje'deki boş .NET 9 iskeleti de .NET 10'a yükseltilir. Makinede SDK 10.0.102 kurulu.
- **Hosting:** PRJ'ye ait, Asprubber'dan ayrı bir Windows/IIS hosting olacak; MS SQL desteği var (kullanıcı onayladı). .NET 10 Hosting Bundle gerekir.
- **Eski site erişimi:** Kullanıcı WordPress yönetici bilgilerini paylaşacak. İçerik oradaki dışa aktarma dosyasından ve REST uç noktalarından alınır. Kimlik bilgileri repoya, belgeye ya da koda yazılmaz.
- **Domain:** prestijteknikmakine.com.tr korunur. Ayrı bir domain alınırsa yalnızca ayar değişir.
- **Repo:** `C:\Users\çağan\source\repos\PrjProje` (mevcut `PrjProje.sln`). Henüz git deposu değil; `git init` ile başlatılır.

## 4. Yaklaşımlar

| | Yaklaşım | Artı | Eksi |
|---|---|---|---|
| **A (seçildi)** | asprubberT kodunu PrjProje'ye kopyalayıp PRJ'ye uyarlamak. Gereksiz modüller silinir, isim alanı `PrjProje` olur, tasarım baştan yapılır. | Kanıtlanmış altyapı (iki dilli rotalar, panel, formlar, güvenlik); en hızlı ve en düşük riskli yol | Asprubber kalıntıları titizlikle temizlenmeli |
| B | Boş .NET iskeletine sıfırdan yazmak | Temiz kod tabanı | Birkaç kat daha uzun sürer, yeni hata riski taşır |
| C | WordPress'te kalıp yeni tema giydirmek | Hosting değişmez | Panel ve iki dilli SEO kalitesi düşük kalır, kullanıcının teknoloji yığınına uymaz |

Kullanıcı A'yı seçti.

## 5. Kurumsal kimlik

Logo: kırmızı, kalın, geometrik "PRJ" monogramı (J'nin ucunda krem/sarı küçük bir dikdörtgen vurgu var) ve altında serif yazıyla "PROJE MAKİNA A.Ş" yazısı.

**Sektör analizi.** Toz boya ekipmanı alıcıları sanayi tesisleri, alüminyum/profil üreticileri, metal mobilya, otomotiv yan sanayi ve beyaz eşya. Global rakiplerin dili (Gema, Wagner, Nordson, Sames) dört şeyden oluşur: koyu ya da nötr zemin, tek güçlü marka rengi, gerçek ürün ve tesis fotoğrafları, teknik güven unsurları (sertifika, referans, anahtar teslim süreç). PRJ de bu dili kullanır: "mühendislik güveni + yerli üretici çevikliği".

**Kimlik bileşenleri**
- **Logo seti:** Mevcut PNG'den vektörel (SVG) yeniden çizim. Monogram ve yazı birlikte ana logoyu oluşturur. Ayrıca yatay versiyon, yalnız monogram, negatif (koyu zemin üstü beyaz), tek renk, favicon/app ikonları (16–512) ve OG görsel şablonu hazırlanır. Logo formu değiştirilmez, yalnızca temizlenir.
- **Renkler:**
  - PRJ Kırmızı: logodan örneklenen ana renk.
  - Antrasit #1C2127: ana koyu zemin ve metin.
  - Çelik gri skalası: arayüz ve çizgiler.
  - Beyaz.
  - Toz Krem: J vurgusundan türetilen, çok sınırlı kullanılan ikincil vurgu.
  - Kırmızının tonları ve erişilebilir kontrast (WCAG AA) kuralları.
- **Tipografi:** Başlıklarda Barlow / Barlow Condensed (endüstriyel ve teknik), metinlerde Inter. Fontlar kendi sunucumuzdan yayınlanır ve Türkçe karakter desteklidir.
- **Görsel dil:**
  - İnce çizgi grid ve teknik çizim motifleri.
  - Toz bulutu/parçacık dokusu, yalnızca hero bölümünde ve düşük yoğunlukta.
  - Ürün görselleri beyaz ya da açık gri kartlarda, tutarlı oranda gösterilir.
  - İkonlar tek çizgi setinden seçilir.
- **Teslimat:**
  - Sitede `wwwroot/css` içinde tasarım token'ları.
  - `docs/brand/` altında marka rehberi: HTML sayfa ve PDF. Logo kullanımı, boşluk, yanlış kullanımlar, renkler, tipografi ve örnek uygulamalar (antetli kağıt, kartvizit, e-posta imzası, sosyal medya görseli) yer alır.
  - `docs/brand/logo/` içinde logo dosyaları.

**Ana marka adı:** "PRJ Proje Makina A.Ş." (kullanıcı onayladı). "Prestij Teknik Makine" yalnızca domain ve eski ad olarak SEO metinlerinde geçer.

**Logo kaynağı:** Kullanıcının verdiği dosya `PrjProje/wwwroot/image/logo.png`: 275×200 px, raster (eski sitedekiyle aynı). Logonun vektörel (SVG) hali bu dosyadan birebir yeniden çizilir; form değiştirilmez.

**Kırmızı (uygulayıcı kararı):** Logodaki kırmızı saf #FF0000 (Photoshop'un varsayılan kırmızısı). Bu renk logo dosyalarında aynen korunur. Ancak beyaz yazıyla kontrastı ancak ~4:1, yani WCAG AA'nın altında. Bu yüzden arayüzdeki "PRJ Kırmızı" (buton, link, vurgu) bir ton derinleştirilir: #E30613, beyazla kontrastı ≥ 4.5:1. Hover rengi #B8050F olur.

## 6. Site haritası (TR / EN)

| Sayfa | TR | EN |
|---|---|---|
| Ana sayfa | `/tr` | `/en` |
| Kurumsal | `/tr/kurumsal` | `/en/about-us` |
| Belgeler ve sertifikalar | `/tr/belgeler` | `/en/certificates` |
| Ürünler | `/tr/urunler` | `/en/products` |
| Kategori | `/tr/urunler/kategori/{slug}` | `/en/products/category/{slug}` |
| Ürün detay | `/tr/urunler/{slug}` | `/en/products/{slug}` |
| Hizmetler | `/tr/hizmetler` | `/en/services` |
| Hizmet detay | `/tr/hizmetler/{slug}` | `/en/services/{slug}` |
| Videolar ve galeri | `/tr/videolar` | `/en/videos` |
| Kullanım kılavuzları ve kataloglar | `/tr/kullanim-kilavuzlari` | `/en/user-manuals` |
| Blog (yazı olana kadar menüde gizli) | `/tr/blog` | `/en/blog` |
| Teklif al | `/tr/teklif-al` | `/en/request-a-quote` |
| İletişim | `/tr/iletisim` | `/en/contact` |
| KVKK / Çerez | `/tr/kvkk`, `/tr/cerez-politikasi` | `/en/privacy-policy`, `/en/cookie-policy` |

Hizmet detay sayfaları: anahtar teslim toz boya tesisi kurulumu, teknik servis ve bakım, yedek parça, özel imalat. Kesin liste mevcut `/hizmetler/` sayfasından alınır.

**Ana sayfa akışı:**
1. Hero: güçlü başlık, gerçek tesis veya ürün görseli, "Teklif Al" ve "Ürünleri İncele" butonları.
2. Ürün kategorileri grid'i.
3. Anahtar teslim tesis süreci (keşif → projelendirme → imalat → kurulum → servis).
4. Neden PRJ: rakamlar yalnızca gerçek veri varsa gösterilir.
5. Öne çıkan ürünler.
6. Video.
7. Belgeler ve referans logoları (varsa).
8. Teklif çağrısı (CTA) bandı.

**Ürün detay sayfası:**
- Galeri (yakınlaştırmalı).
- Kısa açıklama.
- Model ve varyant listesi (örneğin B 300 / B 400 / B 500 / YB 400 / YB 500).
- Teknik özellik tablosu (yalnızca veri varsa).
- Uzun açıklama, uygulama alanları ve indirilebilir kılavuz.
- Sabit "Bu ürün için teklif al" butonu: form ürün adıyla dolu açılır.
- WhatsApp butonu: hazır mesajda ürün adı yazılı gelir.
- İlgili ürünler.

**Global öğeler:**
- Sabit header: mega menüde 9 kategori, TR/EN bayraklı dil menüsü, telefon ve "Teklif Al" butonu.
- Sağ altta WhatsApp butonu.
- Mobilde alt bar: Ara (telefon) · WhatsApp · Teklif.
- Footer: iletişim, kategoriler ve sosyal medya hesapları (Instagram, Facebook, YouTube).
- Çerez onayı ve buna bağlı GA4.

## 7. Yönetim paneli (`/admin`)

asprubberT'den uyarlanan modüller:
- Dashboard: son teklifler, mesajlar, WhatsApp tıklama sayısı.
- Ürünler ve kategoriler: TR/EN sekmeleri, slug, meta başlık ve açıklama, çoklu görsel ve sıralama, teknik tablo, model listesi, öne çıkarma ve aktif/pasif.
- Hizmetler: asprubberT'deki "Capabilities" modülü yeniden adlandırılarak kullanılır.
- Belgeler, kılavuzlar ve kataloglar (PDF).
- Galeri ve videolar (YouTube bağlantısı).
- Referanslar.
- Blog.
- Site metinleri: .resx'teki metinler panelden değiştirilebilir.
- Teklif talepleri ve iletişim mesajları.
- Site ayarları: iletişim bilgileri, WhatsApp numarası ve TR/EN hazır mesajlar, SMTP, GA4 kimliği, sosyal medya bağlantıları.
- Kullanıcılar.

**Roller:** Yönetici (her şey) ve Editör (içerik).

Görsel yüklemede otomatik WebP dönüşümü ve boyutlandırma yapılır (SkiaSharp). Alt metin alanı zorunludur.

## 8. SEO

- Her dil kendi URL'sine sahip olur. Sayfalarda hreflang tr/en/x-default ve canonical bulunur.
- Benzersiz `<title>` ve meta açıklama. Panelde boş bırakılırsa otomatik üretilir.
- JSON-LD: Organization + LocalBusiness (adres, telefon, harita), Product (marka "PRJ", görseller, açıklama; fiyat yok), BreadcrumbList, VideoObject.
- Çok dilli `sitemap.xml` ve `robots.txt`. OG ve Twitter kartları.
- Performans: WebP, `srcset`, lazy-load, kritik CSS, sıkıştırma, cache başlıkları. Hedef Lighthouse SEO, Erişilebilirlik ve En İyi Uygulamalar skorları 100, Performans ≥ 90 (mobil).
- **301 haritası:**
  - `/urunler/<eski-slug>/` → `/tr/urunler/<yeni-slug>`
  - `/urun-kategori/<slug>/` → kategori sayfası
  - `/kurumsal/`, `/iletisim/`, `/belgeler/`, `/hizmetler/`, `/videolar/`, `/kullanim-kilavuzu/`, `/cihazlar*/` → yeni karşılıkları
  - GTranslate'in `?lang=en` biçimli adresleri → `/en/...`
  - Harita panelden düzenlenebilir olur.
- **Dürüst not:** "%100 SEO uyumlu" ifadesi teknik SEO'nun eksiksiz yapılması demektir. Google sıralaması garanti edilemez.

## 9. İçerik taşıma

1. Tek seferlik bir taşıma betiği yazılır (repoda `tools/` altında, sitenin parçası değil). Betik mevcut sitedeki 9 kategoriyi, tüm ürünleri, orijinal çözünürlükteki görselleri, kurumsal, belgeler, hizmetler, videolar ve kılavuz içeriklerini çeker ve seed verisine (JSON) dönüştürür.
2. Her ürün için özgün bir TR metni yazılır: kısa açıklama, uzun açıklama, uygulama alanları, meta başlık ve açıklama. Ardından profesyonel bir EN karşılığı hazırlanır. Teknik bilgi yalnızca kaynakta olandan alınır.
3. Görsellerin dosya adları SEO uyumlu hale getirilir, WebP'ye çevrilir ve TR/EN alt metinleri eklenir.

**Netleşen konular (uygulayıcının kararı):**
- **Metinler:** Tüm TR/EN metinleri biz yazıyoruz. Yayından önce firma sahibi metinleri panelden kontrol edip onaylıyor; teknik doğruluk onun sorumluluğunda.
- **Kaynak liste:** Taşınacak içeriğin ve eski URL'lerin tam listesi önce WordPress'in herkese açık `wp-sitemap.xml` dosyasından ve WP/WooCommerce REST uç noktalarından alınır. Menüden bağlantı verilmeyen sayfalar da böylece yakalanır. WP yönetici erişimi verilirse dışa aktarma dosyası tercih edilir.
- **Yasal metinler:** KVKK, gizlilik ve çerez metinleri firma bilgileriyle doldurulmuş şablonlar olarak hazırlanır. Yayından önce firmanın ya da hukuk danışmanının onayı gerekir.
- **Arama:** Mobil alt bardaki "Ara" telefonla arama anlamındadır (`tel:` bağlantısı). Ürünler sayfasında ayrıca basit bir ürün arama kutusu bulunur; arama sonuç sayfaları `noindex` olur.
- **Logo kaynağı:** Firmada orijinal yüksek çözünürlüklü ya da vektörel logo dosyası varsa o kullanılır. Yoksa sitedeki PNG'den yeniden çizilir; kırmızının tam değeri o dosyadan örneklenip onaya sunulur.

## 10. Test ve doğrulama

- Birim testleri: route ve slug üretimi, hreflang, 301 haritası, form doğrulama ve spam koruması. asprubberT.Tests yapısı örnek alınır.
- Playwright ile kontroller: TR/EN her sayfa türü, mobil ve masaüstü ekran görüntüleri, formun e-posta göndermesi, WhatsApp bağlantısı ve eski URL yönlendirmeleri.
- Lighthouse ölçümü ve Rich Results / schema doğrulaması.

## 11. Uygulayıcıya bırakılanlar

Bileşen düzeyindeki görsel detaylar, animasyonlar (ölçülü, `prefers-reduced-motion` uyumlu), kod organizasyonu, test kapsamının ayrıntısı ve seed verisinin biçimi.
