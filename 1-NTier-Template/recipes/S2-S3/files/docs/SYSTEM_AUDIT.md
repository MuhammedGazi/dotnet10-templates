# 🔍 KAPSAMLI SİSTEM DENETİM RAPORU TALEBİ (.NET 10 N-Tier)

> [!IMPORTANT]
> **Ön Koşul ve Referanslar:** Bu denetim metodolojisi uygulanırken projenin resmi mimari anayasası olan **[`README.md`](../README.md)** ve geliştirme iş akışı rehberi olan **[`docs/AI_WORKFLOW.md`](./AI_WORKFLOW.md)** temel doğruluk kaynağı (*Single Source of Truth*) olarak kabul edilir.

## 👤 ROL VE BAKIŞ AÇISI
Sen kıdemli bir `.NET 10` sistem denetim ekibisin. Aşağıdaki boyutları analiz ederken her bölümde hangi persona ile baktığını açıkça belirt:
- **Bölüm 1-2:** Kıdemli .NET Yazılım Mimarı bakışı
- **Bölüm 3:** DevSecOps & Güvenlik Uzmanı bakışı
- **Bölüm 4:** QA / Test Mühendisi bakışı
- **Bölüm 5:** SRE / DevOps & Veritabanı Performans Uzmanı bakışı

Tavrın eleştirel, çözüm odaklı ve **somut** olmalı. Genel geçer tavsiyelerden kaçın; her bulgu doğrudan C# koduna, `.csproj` yapılandırmasına veya `appsettings.json` kanıtına dayansın.

---

## 📂 TARAMA STRATEJİSİ

### Adımlar
1. Önce **çözüm ve dizin ağacını** çıkar (`ProjectApp.slnx`, `Directory.Build.props`, katmanlar ve dosya sayıları).
2. **Giriş noktalarını ve DI/Pipeline yapılandırmasını** (`ProjectApp.API/Program.cs`, `ProjectApp.API/Extensions/*.cs`, `ProjectApp.Services/Extensions/ServiceExtensions.cs`, `ProjectApp.Repositories/Extensions/RepositoryExtensions.cs`) önce incele.
3. Sonra **İş Mantığı (`ProjectApp.Services/Features/*`)** ve **Veri Erişim (`ProjectApp.Repositories/*`)** modüllerini analiz et.
4. Büyük dosyaları (>300 satır) ve karmaşık sorguları ayrı incele.
5. Tarayamadığın dosyaları raporun başında **"İncelenmedi"** listesinde belirt; nedenini yaz (boyut, encoding, binary vb.).

### Hariç Tutulacaklar
```text
bin/, obj/, .vs/, .git/, .idea/, .vscode/
Migrations/ (sadece toplam sayısını ve bekleyen şema farkı olup olmadığını raporla)
logs/, *.log, tmp/, TestResults/
*.Designer.cs, *.g.cs, *.AssemblyInfo.cs (otomatik üretilen derleyici dosyaları)
*.user, *.suo
```

---

## 📏 ÖLÇÜM EŞİKLERİ VE ANAYASA KURALLARI (Bunları aşan her yapıyı raporla)

### A) Kod Metrik Eşikleri

| Metrik | Eşik |
|--------|------|
| Cyclomatic complexity | > 10 |
| Kognitif karmaşıklık | > 15 |
| Metot uzunluğu | > 50 satır |
| Sınıf uzunluğu | > 300 satır |
| Dosya uzunluğu | > 500 satır |
| Metot parametre sayısı (DI Primary Constructor hariç) | > 5 |
| İç içe blok derinliği (`if/for/try`) | > 4 |
| Sınıf metodu sayısı | > 20 |
| Bir dosyanın `using` sayısı | > 25 |

### B) [`README.md`](../README.md) Mimari Anayasa Eşikleri (Sıfır Tolerans)

| Kural | İhlal Durumunda Öncelik |
|-------|-------------------------|
| **Yasaklı Paketler:** `AutoMapper`, `MediatR`, `Swashbuckle.AspNetCore`, `MassTransit`, `Finbuckle.MultiTenant`, `Ardalis.Specification` varlığı | 🔴 **KRİTİK** |
| **Katman Atlama:** `ProjectApp.API` Controller sınıflarından doğrudan `AppDbContext` çağrılması | 🔴 **KRİTİK** |
| **İstisna Fırlatma Yasağı:** `ProjectApp.Services` içinde iş kuralları için `throw new Exception(...)` kullanılması (`ServiceResult.Fail` yerine) | 🟠 **YÜKSEK** |
| **Controller Sızıntısı:** Controller'ların `CustomBaseController` kullanmaması veya dışarıya doğrudan `Entity` açması | 🟠 **YÜKSEK** |
| **Kimlik Standardı:** Entity/DTO'larda `Guid` yerine `int` kullanılması veya yeni kayıtlarda `Guid.CreateVersion7()` yerine `Guid.NewGuid()` çağrılması | 🟠 **YÜKSEK** |

---

## 🎯 ÖNCELİK TANIMLARI

- 🔴 **KRİTİK:** Veri kaybı, güvenlik açığı (SQLi, Auth bypass, Secret leak), production çökme riski, Mimari Anayasa yasaklı paket/katman ihlali. → **Bugün düzelt.**
- 🟠 **YÜKSEK:** Performans/güvenilirlik ciddi etkisi (N+1, bellek içi tablo çekme, DbContext concurrency), refactor'u zorlaştıran teknik borç. → **Bu sprint.**
- 🟡 **ORTA:** Bakım maliyeti yüksek, DRY/SOLID ihlali, okunabilirlik düşük, eksik validasyon. → **Sonraki sprint.**
- 🟢 **DÜŞÜK:** Stil, isimlendirme, C# 14 / .NET 10 modern dil özelliklerini kullanmama, kozmetik. → **Fırsat oldukça.**

---

## 📋 HER BULGU İÇİN ZORUNLU ŞABLON

````markdown
### [🔴/🟠/🟡/🟢] Bulgu Başlığı
- **Persona:** Kıdemli .NET Yazılım Mimarı / DevSecOps / QA / SRE
- **Dosya/Konum:** `Katman/DosyaYolu.cs:Satır`
- **Kategori:** Kod Kalitesi / Mimari / Güvenlik / Test / Production
- **Sorun:** (2-3 cümle net teknik açıklama ve varsa ihlal edilen `README.md` maddesi)
- **Etki:** (Somut senaryo: bu sorun ne zaman, hangi yükte veya hangi girdiyle patlar?)
- **Kanıt:**
```csharp
// mevcut sorunlu C# kodu veya yapılandırma
```
- **Çözüm:**
```csharp
// önerilen .NET 10 / mimari uyumlu düzeltme
```
- **Düzeltme Eforu:** S (≤1sa) / M (1-4sa) / L (1-3 gün) / XL (haftalar)
- **Bağımlılık:** (Bu düzeltmenin başka hangi katmanları, DTO'ları veya uç noktaları etkileyebileceği)
````

---

## 🔬 ANALİZ BOYUTLARI

### 1. YAZILIM KALİTESİ VE KOD ANALİZİ (Kıdemli .NET Yazılım Mimarı)
- **Karmaşıklık:** Eşikleri aşan metotlar ve servis sınıfları.
- **Anti-Pattern Taraması:** God Object, Anemic/Leaky Services, Magic Numbers/Strings, Long Method, Feature Envy, Shotgun Surgery, Primitive Obsession.
- **Ölü Kod:** Kullanılmayan değişkenler, çağrılmayan servis metotları, gereksiz `using` ifadeleri, `.csproj` içinde kullanılmayan NuGet paketleri.
- **Okunabilirlik ve Standartlar:** `<Nullable>enable</Nullable>` uyarıları, DTO'ların `record` olarak tanımlanması, Primary Constructor kullanımı, isimlendirme tutarlılığı (`Async` soneki vb.).
- **Bellek ve Performans (EF Core & .NET 10):**
  - Salt okunur sorgularda `AsNoTracking()` kullanımı.
  - `ToListAsync()` sonrası bellekte filtreleme yerine `IQueryable` + Mapster `.ProjectToType<TDto>().ToPagedResultAsync(request)` kullanımı.
  - Asenkron metotlarda `CancellationToken` aktarımı ve `.Result` / `.Wait()` (sync-over-async) bloklamalarının olmaması.
- **Dokümantasyon:** `README.md`, `docs/AI_WORKFLOW.md` ve `docs/wiki/` dosyalarının gerçek kodla %100 uyumu.
- **Yerel Bağlam Kontrolleri (.NET & Türkçe Özelinde):**
  - **Türkçe `'İ/i'` ve `'I/ı'` Sorunu:** Kültür bağımlı `.ToLower()` / `.ToUpper()` yerine `.ToLowerInvariant()`, `.ToUpperInvariant()` veya `StringComparison.OrdinalIgnoreCase` kullanımı.
  - **Tarih/Saat:** `DateTime.Now` yerine her yerde `TimeProvider` / UTC kullanımı (`AuditDbContextInterceptor`, JWT `Expires`, RefreshToken süreleri).
  - **Para/Sayı Formatı:** Fiyat alanlarında `decimal` kullanımı ve EF Core `IEntityTypeConfiguration` içinde `HasPrecision(18, 2)` tanımının varlığı.

### 2. MİMARİ VE TASARIM (Kıdemli .NET Yazılım Mimarı)
- **Prensip Uyumu:** SOLID, DRY, KISS, YAGNI ihlalleri (somut C# örnekleriyle).
- **Bağımlılık Yönetimi ve DI Yaşam Döngüleri:**
  - `Scoped`, `Singleton`, `Transient` kayıtlarının doğruluğu (*Captive Dependency* riski: `Singleton` bir servisin `Scoped` bir servisi veya `AppDbContext`'i tutması).
- **Katmanlama (`API` → `Services` → `Repositories`):**
  - Katmanlar arası sızıntı var mı? (`API` Controller sınıflarının `Repositories` içindeki entity/veritabanı nesnelerine doğrudan dokunması, `Services` katmanının HTTP/Controller tiplerine bağımlı olması).
- **Tasarım Desenleri ([`README.md`](../README.md) Uyumluluğu):**
  - `Feature-Service` (`I<Entity>Service` → `<Entity>Service`), `ServiceResult<T>`, `CustomBaseController`, `FluentValidationFilter`, `AuditDbContextInterceptor`, `GlobalExceptionHandler` ve `Mapster` (`IRegister`) desenlerinin doğru ve eksiksiz uygulanması.
- **Genişletilebilirlik:** Yeni bir Feature eklendiğinde mevcut kodları kırmadan (`Open/Closed`) eklenebiliyor mu?

### 3. GÜVENLİK — DevSecOps (ASP.NET Core & Identity)
- **Sır Yönetimi (Secret Management):**
  - `appsettings.json` veya kaynak kod içinde hardcoded `Jwt:SigningKey`, veritabanı şifresi veya API anahtarı var mı?
  - `.gitignore` dosyası hassas yerel ayarları koruyor mu?
- **Girdi Doğrulama (Input Validation):**
  - Tüm `Request` modelleri için `FluentValidation` (`AbstractValidator<T>`) kuralları eksiksiz mi?
  - Ham SQL sorgusu varsa parametreli mi (SQL Injection riski)?
- **Kimlik Doğrulama ve Erişim Kontrolü (AuthN & AuthZ):**
  - `[Authorize]` ve `[Authorize(Roles = "Admin")]` öznitelikleri doğru uç noktalarda kullanılmış mı?
  - `ICurrentUserService` üzerinden kullanıcı kimliği güvenli okunuyor mu? Kullanıcı başkasının verisini manipüle edebilir mi (IDOR riski)?
  - Refresh Token rotasyonu, süresi, iptali (`RevokeRefreshTokenAsync`) ve access token security stamp doğrulaması (`OnTokenValidated`) güvenli mi?
- **Kriptografi ve Güvenlik Başlıkları:**
  - `Identity` parola kuralları, hesap kilitleme (`Lockout`), 512-bit kriptografik Refresh Token üretimi (`RandomNumberGenerator`).
  - `SecurityHeadersExtensions` (`X-Content-Type-Options`, `X-Frame-Options`, `CSP`), `CorsExtensions` (Production'da `AllowAnyOrigin` olmaması) ve `RateLimiterExtensions` (`auth-policy` Brute-Force koruması).
- **Bağımlılık Güvenliği:**
  - `.csproj` dosyalarında bilinen güvenlik açığı (CVE) barındıran veya gereksiz yere eklenmiş paketler.

### 4. TEST VE KAPSAM (QA / Test Mühendisi)
- **Mevcut Durum:** Çözümdeki birim (Unit) ve entegrasyon (Integration) testlerinin durumu, test piramidi dengesi.
- **Test Edilebilirlik:**
  - Servislerin `AppDbContext` ile SQLite in-memory (`TestDb`) üzerinden test edilebilirliği; bağımlılıkların (`IMapper`, `ICurrentUserService`, `TimeProvider`) test ortamında doğru yapılandırılması; `static` veya sıkı bağlı çağrıların testi engelleyip engellemediği.
- **Edge Case'ler:**
  - Boş/null/aşırı uzun girdi, negatif fiyat/stok, `Guid.Empty`, aynı anda gelen paralel istekler, silinmiş (`IsDeleted = true`) kayıtlara erişim veya tekrar ekleme denemeleri.
- **Hata Yönetimi (Error Handling):**
  - Sessizce yutulan `catch (Exception) { }` blokları var mı?
  - İş hataları (`ServiceResult.Fail`) ile beklenmeyen sistem hataları (`CriticalExceptionHandler` & `GlobalExceptionHandler` → `ServiceResult` 500 yanıtı) net ayrılmış mı? Stack trace dışarıya sızıyor mu?
- **En Kritik 5 Servis Metodu İçin:** Yazılması gereken `xUnit` (`Assert`) test senaryoları önerisi (`FluentAssertions` yasaklıdır, README §1.1).

### 5. PRODUCTION VE DAĞITIM (SRE / DevOps)
- **Çevre Yapılandırması (`Development` vs `Production`):**
  - `IdentityDataSeeder` (otomatik DB oluşturma ve varsayılan `admin@admin.com` tohumlama) ve `Scalar UI` (`/scalar/v1`) yalnızca `IsDevelopment()` altında mı çalışıyor? Production'a sızma riski var mı?
  - `IOptions<T>` (`ValidateDataAnnotations`, `ValidateOnStart`) ile eksik konfigürasyonların uygulama başlangıcında yakalanıp yakalanmadığı.
- **Loglama ve İzlenebilirlik (Observability):**
  - `Serilog` yapılandırması, `X-Correlation-Id` zenginleştirmesi (`UseCorrelationId()`), doğru log seviyeleri (`Information`, `Warning`, `Error`, `Critical`).
  - Loglara parola, JWT token veya kişisel veri (PII) sızıp sızmadığı.
- **Performans Darboğazları ve Dayanıklılık:**
  - EF Core N+1 sorgu riskleri, eksik veritabanı indeksleri (`CategoryConfiguration`, `ProductConfiguration`), büyük listelerde sayfalama (`PagedResult<T>`) zorunluluğu, Connection Pool yönetimi.
- **Dağıtım, Build ve Sağlık Kontrolleri:**
  - `dotnet build` ve `dotnet publish` komutlarının tek adımda hatasız çalışması.
  - `/health/ready` uç noktasının (`AddSqlServer`) veritabanı erişilebilirliğini doğru raporlaması (`/health` yalnızca liveness'tır).

---

## 📤 RAPOR ÇIKTI YAPISI

Raporu **tam olarak şu sırayla** üret:

### Bölüm A — Yönetici Özeti (1 sayfa)
Tek paragraflık genel mimari değerlendirme + aşağıdaki özet tablo:

| # | Bulgu | Öncelik | Boyut | Efor | Etki |
|---|-------|---------|-------|------|------|
| 1 | ... | 🔴 | Güvenlik / Mimari | M | Yüksek |

### Bölüm B — Korunması Gereken İyi Pratikler (Pozitif Bulgular)
Projede doğru uygulanmış 3-5 mimari/kalite kararını (`ServiceResult<T>`, `AuditDbContextInterceptor`, `Guid.CreateVersion7()`, `Scalar + OpenAPI`, `RateLimiter` vb.) somut dosya referanslarıyla belirt. Refactor sırasında **"neye dokunmayalım / neyi koruyalım"** rehberi olarak.

### Bölüm C — İncelenmedi Listesi
Tarayamadığın veya kapsam dışı bırakılan dosyalar ve nedenleri.

### Bölüm D — Detaylı Bulgular
Yukarıdaki zorunlu bulgu şablonuna göre, **öncelik sırasıyla** (🔴 Kritik → 🟠 Yüksek → 🟡 Orta → 🟢 Düşük). Her bulguda ilgili persona'yı belirt.

### Bölüm E — Kriz Yönetimi / Aksiyon Planı

#### 🚨 24 SAAT İÇİNDE (Yangın Söndürme)
En kritik 3 madde. Her biri için:
- **Eylem:** Ne yapılacak?
- **Beklenen Sonuç:** Hangi risk ortadan kalkacak?
- **Bağımlılık:** Hangi katman/dosya etkilenir?
- **Risk:** Bu adım neyi bozabilir ve nasıl doğrulanır (`dotnet build` / test)?

#### 📅 BU HAFTA (Stabilizasyon)
5 madde, aynı format.

#### 🗓 BU AY (Sağlamlaştırma)
5 madde, aynı format.

### Bölüm F — Sonraki Adımlar
Raporun sonunda kullanıcıya şu soruyu yönelt:
> "Hangi bulgudan başlamak istersin? Seçtiğin maddenin detaylı refactor planını, etkilenecek dosyaları ve örnek 'before/after' C# kodunu üretebilirim."

---

## 📐 ÇIKTI HEDEFLERİ

- **Toplam uzunluk:** ~2000-4000 kelime
- **Bulgu sayısı:** En kritik 15-25 bulgu (her şeyi listeleme; sinyal/gürültü oranı yüksek olsun)
- **Dil:** Türkçe (C# kod örnekleri ve sembol adları orijinal dilinde)
- **Ton:** Eleştirel ama yapıcı, somut, kanıta dayalı
- **Yasaklı:** "Belki", "olabilir", "genelde" gibi bulanık ifadeler. Kanıtlayamadığın varsayımı rapora ekleme.

---

**Denetim Başlangıcı:** Bu rapor talep edildiğinde önce çözüm dizin ağacını çıkararak tarama planını paylaş, ardından raporu üret.
