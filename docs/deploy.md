# Yayına alma (IIS)

Bu belge PRJ Proje Makina sitesinin bir Windows sunucuda IIS üzerinde yayına alınmasını anlatır.

## 1. Sunucu hazırlığı

1. **.NET 10 Hosting Bundle** kurun (ASP.NET Core Runtime ile IIS modülü `AspNetCoreModuleV2` birlikte gelir). Kurulumdan sonra `iisreset` çalıştırın.
2. **SQL Server** (Express yeterli): boş bir veritabanı ve yalnızca o veritabanında `db_owner` yetkisi olan bir SQL kullanıcısı oluşturun.
3. IIS'te yeni bir site açın. Uygulama havuzu için **.NET CLR sürümü: Yönetilen kod yok**, **Yönetilen ardışık düzen: Tümleşik** seçin.
4. Uygulama havuzu kimliğine (`IIS AppPool\<havuz adı>`) site klasöründe okuma, `wwwroot\uploads`, `App_Data` ve `logs` klasörlerinde yazma izni verin.

## 2. Yayın paketi

```powershell
dotnet publish PrjProje/PrjProje.csproj -p:PublishProfile=PrjFolder
```

Çıktı `PrjProje/bin/Publish/PrjFolder/` altındadır (framework-dependent, `win-x64`). Bu klasörün içeriğini sunucudaki site klasörüne kopyalayın.
Paket `SeedContent/` klasörünü de içerir: eski siteden taşınan kategori, ürün, görsel ve yönlendirmeler ilk açılışta boş veritabanına otomatik yüklenir.
Sonraki açılışlarda yalnızca eksik kayıtlar eklenir; panelde yapılan değişikliklerin üzerine yazılmaz.

## 3. Gizli ayarlar

Şifreler dosyaya yazılmaz. IIS Yöneticisi → site → **Yapılandırma Düzenleyicisi** → `system.webServer/aspNetCore` → `environmentVariables` altına ekleyin
(ya da sunucuda sistem ortam değişkeni olarak tanımlayın):

| Değişken | Örnek |
|---|---|
| `ConnectionStrings__DefaultConnection` | `Server=.\SQLEXPRESS;Database=PrjProje;User Id=prj;Password=…;TrustServerCertificate=True` |
| `Site__BaseUrl` | `https://www.prestijteknikmakine.com.tr` |
| `SeedAdmin__Email` | İlk yönetici e-postası |
| `SeedAdmin__Password` | İlk yönetici şifresi (ilk girişte değiştirilmesi istenir) |

İlk yönetici oluştuktan sonra `SeedAdmin__*` değişkenlerini silebilirsiniz. Geliştirme makinesinde aynı anahtarlar `dotnet user-secrets` ile verilir.

SMTP, WhatsApp numarası, sosyal medya adresleri ve Google Analytics kimliği panelden (**Ayarlar**) girilir.

## 4. İlk açılış kontrolü

1. Siteyi tarayıcıda açın: `/tr` ve `/en` gelmeli. Hata olursa `logs\stdout_*.log` dosyasına bakın.
2. `/admin` ile giriş yapıp şifreyi değiştirin.
3. Site çalıştıktan sonra `web.config` içindeki `stdoutLogEnabled` değerini `false` yapın.
4. HTTPS sertifikasını bağlayın (ör. win-acme ile Let's Encrypt) ve HTTP'den HTTPS'e yönlendirmeyi açın.

## 5. DNS geçişi

1. Geçişten bir gün önce alan adının DNS kayıtlarında TTL değerini 300 saniyeye düşürün.
2. `A` (ve varsa `AAAA`) kaydını yeni sunucunun IP adresine çevirin; `www` için `CNAME` veya ikinci bir `A` kaydı tanımlayın.
3. Geçişten sonra eski WordPress adreslerinden birkaçını deneyin (ör. `/urunler/manuel-corona-cihaz-seti/`, `/kurumsal/`): yeni sayfaya **301** ile gitmeli.
   Yönlendirmeler panelde **Yönlendirmeler** sayfasında listelenir ve kullanım sayısı orada görünür.
4. Her şey yolundaysa TTL'i eski değerine döndürün.

## 6. Google Search Console

1. Search Console'da `https://www.prestijteknikmakine.com.tr` mülkünü doğrulayın (panelde **Ayarlar → Google Search Console doğrulama kodu** alanı meta etiketini ekler).
2. **Site haritaları** bölümüne `sitemap.xml` adresini gönderin.
3. Eski sitenin adresleri yönlendirildiği için ayrıca "adres değişikliği" bildirimi gerekmez; alan adı aynı kalır.
4. İlk haftalarda **Sayfalar** raporunda 404 veren eski adres çıkarsa panelden yeni bir yönlendirme ekleyin.
