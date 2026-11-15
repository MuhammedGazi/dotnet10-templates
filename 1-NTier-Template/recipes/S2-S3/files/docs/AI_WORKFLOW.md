# 🤖 AI Destekli Geliştirme İş Akışı — .NET 10 N-Tier Metodolojisi

> [!IMPORTANT]
> **Ön Koşul ve Temel Referans:** Bu doküman, proje kök dizinindeki **[`README.md`](../README.md)** (Mimari Rehber ve AI Kurallar Anayasası) belgesinin ayrılmaz bir parçasıdır. Herhangi bir kod incelemesi, planlama veya geliştirme yapmadan önce **`README.md`**, ardından bu belge (**`docs/AI_WORKFLOW.md`**) ve denetim süreçleri için **[`docs/SYSTEM_AUDIT.md`](./SYSTEM_AUDIT.md)** sırasıyla okunmalıdır.

Bu doküman, **ProjectApp (.NET 10 N-Tier)** projesinde yapay zeka kodlama ajanları (Antigravity, Claude Code, Cursor, Windsurf, GitHub Copilot vb.) ile çalışırken kullanılan; **wiki kurulumu, kod inceleme, mantık hatası analizi, büyük özellik planlama, doğrulama ve commit disiplinini** tanımlar.

---

## 1. 📚 Proje Wiki Mekanizması (`docs/wiki/`)

**Amaç:** AI ajanlarının her oturumda projeyi sıfırdan keşfetmesini önlemek; mimari kararları, eklenen modülleri (`Feature-Service`), veritabanı şema değişikliklerini ve iş kurallarını kalıcı ve birbirine bağlı (*cross-referenced*) tutmak.

### 1.1. Dokümantasyon Katmanları
1. **Mimari Anayasa ([`README.md`](../README.md)):** N-Tier katman kurallarını (`ProjectApp.API`, `ProjectApp.Services`, `ProjectApp.Repositories`), yasaklı paketleri ve zorunlu tasarım desenlerini (`ServiceResult<T>`, `CustomBaseController`, `Mapster`, `Guid.CreateVersion7()`) belirleyen temel referans.
2. **Metodoloji ve Denetim Rehberleri (`docs/AI_WORKFLOW.md` & `docs/SYSTEM_AUDIT.md`):** Çalışma disiplinini ve kalite/denetim standartlarını belirleyen kurallar bütünü.
3. **Canlı Proje Wiki'si (`docs/wiki/`):** AI'ın tam kontrole sahip olduğu, Obsidian stili `` `[[sayfa_adi]]` `` bağlantılarıyla birbirine bağlı, projenin yaşayan hafızasını tutan Markdown dosyaları.

### 1.2. Kritik Wiki Dosyaları
- **[`docs/wiki/index.md`](./wiki/index.md)** — Tüm wiki'nin içerik haritası. Kategorilere ayrılmış sayfa listesi + tek cümlelik özet. Yeni bir modül (ör. `Order`, `Payment`) veya mimari karar eklendiğinde buraya işlenir.
- **[`docs/wiki/log.md`](./wiki/log.md)** — Kronolojik kayıt defteri. Her önemli değişiklik dosyanın **en üstüne** şu formatta eklenir:
  ```markdown
  ## [YYYY-AA-GG] [İŞLEM_TİPİ] | Kısa Açıklama
  ```
  İşlem tipleri:
  - `INGEST` (Yeni `Feature-Service`, Entity, kütüphane veya iş kuralı eklendiğinde)
  - `REVIEW` (Kod inceleme, refactoring veya mantık hatası düzeltmesi yapıldığında)
  - `LINT` (Wiki ve dokümantasyon bakım/tutarlılık kontrolü yapıldığında)

### 1.3. Zorunlu Çalışma Akışı
- **Önce Oku:** Kod yazmadan veya mimari karar vermeden önce [`README.md`](../README.md) ve [`docs/wiki/index.md`](./wiki/index.md) okunur, gerektiğinde `` `[[link]]` `` bağlantıları takip edilir. Proje sıfırdan rastgele taranmaz.
- **Proaktif Güncelleme:** Yeni bir Entity, Feature-Service, NuGet paketi, `appsettings.json` yapılandırması veya karmaşık iş kuralı eklendiğinde sadece sohbette bırakılmaz — ilgili wiki sayfası hemen güncellenir/oluşturulur, `index.md` + `log.md` dosyalarına işlenir. Kod değişikliği ile wiki güncellemesi **aynı turda** tamamlanır.
- **Bağlantısallık:** Her wiki sayfasında ilgili diğer sayfalara `` `[[link]]` `` verilir. Öksüz (hiçbir yere bağlanmayan) sayfa bırakılmaz. Bir modül veya dosya silinirse, ona işaret eden tüm linkler de temizlenir (*broken link* bırakılmaz).
- **Syntax Örnekleri:** Dokümantasyon içinde `[[...]]` sözdizimini *örnek olarak* göstermek gerekirse inline code (`` `[[sayfa_adi]]` ``) kullanılır; çift köşeli parantez çıplak bırakılmaz.

### 1.4. Operasyon Komutları

| Komut (Kullanıcı Söyler) | AI Ne Yapar |
|---|---|
| **"Bunu ingest et"** | Kaynağı/kodu analiz et → `docs/wiki/` altında yeni sayfa aç → çelişen/genişleyen mevcut sayfaları güncelle → `index.md`'ye link ekle → `log.md`'ye `INGEST` kaydı düş |
| **Detaylı proje sorusu** | Genel varsayım kullanma; önce `README.md` ve `docs/wiki/index.md` üzerinden ilgili sayfaları oku → proje mimarisiyle sentezlenmiş yanıt ver → kalıcı değerliyse wiki'ye ekle |
| **"Wiki'yi lint et"** | Tüm `docs/wiki/` ve `README.md` referanslarını tara → gerçek C# koduyla çelişen bilgi, güncelliğini yitirmiş karar, öksüz sayfa veya kırık link tespit et → rapor sun → onay sonrası düzelt |

---

## 2. 🔍 Kod İnceleme (Code Review) Süreci

**Tetikleyici Örnek İstem:** *"code review yap: şişkin dosya, god object, ölü kod var mı kontrol et. oop, solid, dry, kiss, design pattern'lara ve README anayasasına aykırılık var mı kontrol et, optimizasyon ve kod karmaşıklığı açısından da kontrol et rapor ver"*

### 2.1. .NET 10 N-Tier Kontrol Listesi
- **Mimari Anayasa ve Yasaklı Paket Kontrolü ([`README.md`](../README.md) §1):**
  - `.csproj` dosyalarında yasaklı paketler (`AutoMapper`, `MediatR`, `Swashbuckle.AspNetCore`, `MassTransit`, `Finbuckle.MultiTenant`, `Ardalis.Specification`) var mı?
  - Tüm Entity, DTO ve Route parametrelerinde `Guid` ve yeni kayıtlarda `Guid.CreateVersion7()` standardına uyulmuş mu?
- **Ölü Kod (Dead Code):**
  - Hiçbir yerde DI ile enjekte edilmeyen veya çağrılmayan servis metotları, DTO/Request/Validator sınıfları veya konfigürasyonlar.
  - `.csproj` dosyalarında kodda hiç kullanılmayan `<PackageReference>` paketleri veya gereksiz `using` ifadeleri.
- **God Object / Şişkin Dosya ve Katman Sınır İhlalleri:**
  - **Controller İhlalleri (`ProjectApp.API`):** Controller içinde iş mantığı, validasyon veya doğrudan `AppDbContext` kullanımı var mı? Tüm Controller'lar `CustomBaseController`'dan miras alıp tek satırda `CreateActionResult(...)` dönüyor mu? Dışarıya doğrudan `Entity` açılmış mı?
  - **Service İhlalleri (`ProjectApp.Services`):** Tek bir servis sınıfı (`ProductService` vb.) kendi sınırları dışındaki birçok farklı modülün sorumluluğunu üstlenmiş mi? İş kuralı hatalarında `throw new Exception(...)` yerine `ServiceResult.Fail(...)` kullanılmış mı?
- **SOLID İhlalleri:**
  - **Single Responsibility (SRP):** Servis ve Konfigürasyon sınıflarının tek bir sorumluluğu olmalı.
  - **Open/Closed (OCP):** Yeni bir iş kuralı veya filtre eklemek için mevcut sınıflarda devasa `if/else` veya `switch` blokları mı değiştiriliyor?
  - **Dependency Inversion (DIP):** `ProjectApp.API` katmanı servis arayüzlerine (`IProductService`, `ICurrentUserService`) mi bağımlı?
- **DRY (Don't Repeat Yourself) İhlalleri:**
  - `AuditDbContextInterceptor` zaten `Created`, `CreatedBy`, `Updated`, `UpdatedBy` ve Soft-Delete (`IsDeleted`) alanlarını otomatik yönetirken, servis içinde bu alanların elle tekrar atanması.
  - `FluentValidationFilter` varken Controller veya Service içinde mükerrer `ModelState` kontrolü yazılması.
  - `Mapster` (`IRegister`) profilleri yerine servis metotları içinde tekrarlayan manuel nesne eşlemeleri.
- **KISS & Magic Values:**
  - Sabit string, rol adı (`RoleNames.Admin` yerine `"Admin"`), bağlantı dizesi veya JWT ayarlarının `IOptions<T>` (`JwtOptions`) yerine kod içine gömülmesi.
- **EF Core Optimizasyon ve Performans:**
  - Salt okunur (Read-Only) sorgularda `AsNoTracking()` kullanılıyor mu?
  - Veritabanından tüm tabloyu belleğe çekip (`ToListAsync()`) ardından C# tarafında filtreleme/sayfalama yapılması yerine `IQueryable` üzerinde Mapster `.ProjectToType<TDto>().ToPagedResultAsync(request)` kullanılıyor mu?
  - Döngü (`foreach`) içinde veritabanı sorgusu (N+1 problemi) veya döngü içinde `context.SaveChangesAsync()` çağrısı var mı?

### 2.2. Rapor Formatı
Her bulgu için:
1. **Konum:** `Katman/DosyaYolu.cs:Satır`
2. **Ne Bozuk:** Hangi prensip veya `README.md` kuralı ihlal edilmiş?
3. **Neden Bozuk / Senaryo:** Hangi çalışma zamanı hatasına, performans darboğazına veya bakım maliyetine yol açar?
4. **Önerilen Düzeltme:** Somut C# kod örneği.
Bulgular önem sırasına göre (**Kritik → Yüksek → Orta → Kozmetik**) listelenir ve rapor sonunda net bir uygulama öncelik sırası verilir.

### 2.3. Onay ve Uygulama Akışı
AI bulguları **uygulamadan önce raporlar** ve kullanıcı onayı bekler (*"onayladım, uygula"* / *"sırayla adım adım yap"*). Onay sonrası:
1. Bulgular sırayla, **her biri ayrı ve izole bir adım** olarak düzeltilir.
2. Her düzeltmeden sonra `dotnet build` ile derleme ve tip güvenliği doğrulanır.
3. Silinen veya yeniden adlandırılan sınıf/metotların `ServiceExtensions.cs`, `RepositoryExtensions.cs`, `README.md` ve `docs/wiki/` içindeki **kalıntı referansları** taranır ve temizlenir.
4. İşlem tamamlandığında [`docs/wiki/log.md`](./wiki/log.md) dosyasına `REVIEW` kaydı düşülür.

---

## 3. 🧠 Mantık Hatası ve Gereksiz İşlem Analizi

Bu aşama, stil ve mimari incelemeden **ayrı bir derinlemesine geçiş** olarak yürütülür.
**Tetikleyici Örnek İstem:** *"projeyi incele mantık hataları, concurrency riskleri veya gereksiz işlemler var mı tespit et"*

### 3.1. Nelere Bakılır?
- **Asenkron & Concurrency / DbContext Çakışmaları:**
  - Aynı `AppDbContext` (Scoped) örneği üzerinde paralel `Task.WhenAll(...)` çalıştırılması (EF Core `DbContext` thread-safe değildir!).
  - Yazma (`Create`, `Update`, `Delete`) işlemlerinden sonra `await context.SaveChangesAsync(cancellationToken)` çağrısının unutulması veya tek bir iş biriminde gereksiz yere birden fazla kez çağrılması.
  - Refresh Token yenileme (`RefreshTokenAsync`) veya stok güncelleme (`UpdateStockAsync`) gibi işlemlerde yarış durumu (*race condition*) riski.
- **Algoritma, Hesaplama ve Sayfalama Hataları:**
  - `PagedRequest` ve `PagedResult<T>` hesaplamalarında `PageNumber` / `PageSize` sınır değerleri (0 veya negatif değerler, `Skip((page - 1) * size)` taşmaları).
  - Fiyat (`decimal`), indirim veya stok hesaplamalarında yuvarlama/işaret hataları.
  - JWT `AccessTokenExpiration` ve `RefreshTokenExpiration` süre hesaplamalarında `DateTime.UtcNow` yerine yerel saat (`DateTime.Now`) kullanılması nedeniyle oluşan saat dilimi kaymaları.
- **Soft-Delete ve Global Query Filter Tutarlılığı:**
  - `AppDbContext` içinde tanımlı `HasQueryFilter(e => !e.IsDeleted)` filtresinin ilişkili tablolarda (ör. silinmiş bir kategorinin ürünlerinin listelenmesi veya aynı isimde silinmiş bir kaydın Unique Index / `AnyAsync` kontrolüne takılması) yarattığı mantıksal çelişkiler.
- **Eksik Girdi Doğrulaması (FluentValidation):**
  - Yeni eklenen `Create*Request` veya `Update*Request` record'ları için `AbstractValidator<T>` sınıfının yazılmamış olması veya sınır kurallarının (boş string, negatif fiyat, geçersiz `Guid.Empty`, eksik `RowVersion`) eksik bırakılması.
- **Gereksiz Sık İşlem:**
  - Aynı metot içinde aynı kaydın hem varlık kontrolü hem de servis içinde tekrar tekrar veritabanından sorgulanması (`AnyAsync` + `FindAsync`/`FirstOrDefaultAsync` optimizasyonu).

### 3.2. Rapor + Onay
Bölüm 2.2 ve 2.3'teki akış birebir izlenir: **Rapor → Kullanıcı Onayı → Sıralı Düzeltme → `dotnet build` Doğrulaması → Wiki `REVIEW` Kaydı.** Yapılan bir düzeltmenin yan etkisi olarak yeni bir uç durum (*edge case*) ortaya çıkarsa gizlenmez, ayrı bir bulgu olarak raporlanır.

---

## 4. 📐 Büyük / Çok Dosyalı Özellikler İçin Plan Modu

**Ne Zaman Kullanılır:** Yeni bir iş modülü/entity eklenmesi, kimlik doğrulama akışının genişletilmesi, önbellekleme/loglama altyapısının değiştirilmesi gibi **3+ dosyayı veya birden fazla katmanı (`Repositories`, `Services`, `API`) etkileyen** isteklerde.

### 4.1. Planlama ve Uygulama Akışı
1. **Netleştirici Sorular:** İstek belirsizse kullanıcıya çoktan seçmeli (önerilen seçenek işaretli) sorular yöneltilir (ör. *"Sipariş (`Order`) modülünde stok düşümü senkron olarak `OrderService` içinde mi yapılsın, yoksa ayrı bir adım olarak mı?"*).
2. **Plan Modu (Salt Okuma):** Kullanıcı onayı alınana kadar hiçbir kaynak kod dosyası değiştirilmez. [`README.md`](../README.md) Bölüm 3'teki **"Yeni Bir Özellik (Feature) Ekleme Adım Adım Kontrol Listesi"** referans alınır.
3. **Somut Plan Dosyası:**
   - **Bağlam (Context):** Özelliğin amacı ve mimari yerleşimi.
   - **Katman Bazlı Dosya Tasarımı:**
     1. `ProjectApp.Repositories`: Entity (`BaseEntity<Guid>`, `IAuditEntity`), `IEntityTypeConfiguration<T>` ve `AppDbContext` `DbSet`.
     2. `ProjectApp.Services`: Request/Response `record` DTO'ları, `AbstractValidator<T>`, Mapster `IRegister` profili, `I<Entity>Service` & `<Entity>Service` (`ServiceResult<T>`), `ServiceExtensions` DI kaydı.
     3. `ProjectApp.API`: `CustomBaseController` tabanlı Controller; okuma uçları anonim, yazma uçları `[Authorize(Roles = RoleNames.Admin)]`.
   - **Sıralı Adım Listesi:** Her adımda hangi dosyanın yazılacağı ve nasıl doğrulanacağı.
   - **Riskler ve Edge Case'ler:** Soft-delete davranışı, Foreign Key ilişkileri, yetki kuralları.
4. **Onay:** Plan kullanıcıya sunulur; onay alınmadan kod yazımına geçilmez.
5. **Sıralı Uygulama:** Plandaki adımlar **birebir sırayla** (`Repositories` → `Services` → `API`) uygulanır ve her katman tamamlandığında derleme kontrolü yapılır.

### 4.2. Kapsam Disiplini (KISS / YAGNI)
Plan yazarken **bilinçli kapsam dışı bırakma** kararları açıkça not edilir (ör. *"Bu aşamada yalnızca CRUD ve sayfalama uç noktaları eklenecektir; Excel dışa aktarma talep edilmediği için kapsam dışı bırakılmıştır"*). Gereksiz soyutlama ve şişkinlikten (*bloat*) kaçınılır.

---

## 5. ✅ Doğrulama ve Test Yöntemleri (.NET 10)

### 5.1. Derleme ve Statik Doğrulama (Her Kod Değişikliğinden Sonra)
Her dosya veya katman değişikliğinden sonra çözüm kök dizininde derleme yapılır:
```powershell
dotnet build ProjectApp.slnx --configuration Debug
```
- Hedef: **0 Hata (0 Errors), 0 Uyarı (0 Warnings)**.
- `<Nullable>enable</Nullable>` açık olduğu için olası `CS8618` / `CS8602` null referans uyarıları görmezden gelinmez, temizlenir.

### 5.2. Web API, Veritabanı ve Uç Nokta Doğrulaması (Canlı Çalışma Testi)
Kod değişikliği çalışma zamanı davranışını (DI kayıtları, EF Core sorguları, Middleware sıralaması, Auth/JWT, Scalar UI) etkiliyorsa yalnızca `dotnet build` ile yetinilmez; uygulama ayağa kaldırılarak doğrulanır:
1. **Uygulamayı Arka Planda Başlat:**
   ```powershell
   dotnet run --project .\ProjectApp.API\ProjectApp.API.csproj --launch-profile http
   ```
2. **Başlangıç ve Seeder Kontrolü:** Konsol/Serilog çıktısında `IdentityDataSeeder` hatası veya DI çözümleme hatası (`InvalidOperationException: Unable to resolve service...`) olmadığını doğrula.
3. **Canlı Uç Nokta Kontrolleri:**
   - Sağlık kontrolü: `Invoke-RestMethod http://localhost:5091/health`
   - OpenAPI şema kontrolü: `Invoke-RestMethod http://localhost:5091/openapi/v1.json`
   - Scalar UI erişimi: `http://localhost:5091/scalar/v1` (veya `/scalar` yönlendirmesi)
   - Kimlik doğrulama ve iş mantığı testi: `/api/auth/login` uç noktasından (`admin@admin.com` / `Password12*`) JWT `accessToken` alıp ilgili yeni uç noktaya gerçek HTTP isteği göndererek `ServiceResult` JSON çıktısını doğrula.
4. **Süreç Temizliği:** Test bittiğinde **yalnızca kendi başlattığın test sürecini** (PID üzerinden) sonlandır; kullanıcının Visual Studio'da açık olan süreçlerine dokunma.
5. **Dürüst Raporlama:** Dış bir servis veya kullanıcının yerel SQL Server ortamına bağlı olup test edilemeyen bir senaryo varsa *"bu kısmı şu nedenle canlı test edemedim, elle kontrol etmeniz gerekiyor"* şeklinde açıkça belirtilir.

---

## 6. 📝 Commit Kuralları

- **Dil:** Tüm commit mesajları **Türkçe** yazılır.
- **Türkçe Karakter Hassasiyeti:** `ı, ğ, ü, ş, ö, ç, İ, Ğ, Ü, Ş, Ö, Ç` harfleri eksiksiz ve doğru kullanılır; ASCII karşılıklarıyla (`i, g, u, s, o, c`) değiştirilmez.
- **AI Referansı Yok:** Commit mesajına veya PR açıklamasına `"Co-Authored-By"`, `"Generated with"`, 🤖 gibi yapay zeka/asistan referansları **kesinlikle eklenmez**.
- **Biçim:** Kısa emir kipinde özet satırı (ör. `Sipariş modülü ve stok doğrulama servisini ekle`), gerekirse boş satırdan sonra madde madde "ne değişti" gövdesi.
- **Commit Öncesi Kontrol:** `git status` ile staged dosyalar gözden geçirilir; `.gitignore` dosyasının `bin/`, `obj/`, `.vs/`, `logs/` gibi geçici çıktıları dışarıda bıraktığı ve hassas bir bilginin (`appsettings.Production.json`, gerçek bağlantı şifreleri) yanlışlıkla eklenmediği doğrulanır.

---

## 7. 🛡️ Genel Prensipler

1. **Anayasaya Tam Sadakat:** [`README.md`](../README.md) içindeki 5 Temel Sistem Kuralı (Yasaklı paketler, `Feature-Service` & `ServiceResult<T>`, `CustomBaseController`, `Guid.CreateVersion7()`, `Mapster`) hiçbir koşulda esnetilemez.
2. **Proaktif Ama Sessiz Değil:** Bir düzeltme yaparken fark edilen yan bulgular (ör. *"bu sırada şu serviste de `AsNoTracking` eksikti, raporluyorum"*) kullanıcıya açıkça bildirilir, gizlenmez.
3. **Kapsamı İstekle Sınırla:** Açıkça istenmeyen genişletmeler yapılmadan önce ya sorulur ya da planda gerekçeli olarak kapsam dışı bırakılır.
4. **Dokümantasyon ve Wiki Her Zaman Güncel:** Kod değişikliği ile `docs/wiki/` (ve gerekirse `README.md`) güncellemesi tek bir bütün sayılır — biri yapılıp diğeri sonraya bırakılmaz.
5. **Silmeden Önce Referans Tara:** Bir sınıf, DTO veya kavram kaldırıldığında DI kayıtları, `README.md` ve `docs/wiki/` içindeki tüm referansları güncellenir; kırık link veya geçersiz bilgi bırakılmaz.
