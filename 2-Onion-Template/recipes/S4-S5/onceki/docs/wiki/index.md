# 🗺️ ProjectApp — Proje Wiki İçerik Haritası (Index)

> [!NOTE]
> Bu dosya, 4 projeli Onion Architecture çözümündeki tüm mimari kararların, kuralların ve modüllerin merkezi haritasıdır. AI ajanları kod yazmadan önce **[`README.md`](../../README.md)** ile birlikte bu indeks dosyasını okumak ve yaptığı değişiklikleri buraya işlemekle yükümlüdür.

---

## 🏛️ 1. Temel Anayasa ve Metodoloji Belgeleri
- **[`README.md`](../../README.md)** — `.NET 10` Onion / Clean Mimari Anayasası, 4 Projeli Katman Sınır Kuralları, Yasaklı Paketler ve Yeni Özellik Ekleme Rehberi.
- **[`docs/AI_WORKFLOW.md`](../AI_WORKFLOW.md)** — AI Destekli Geliştirme İş Akışı, Wiki Yönetimi, Kod İnceleme, Plan Modu, Doğrulama ve Commit Kuralları.
- **[`docs/SYSTEM_AUDIT.md`](../SYSTEM_AUDIT.md)** — Kapsamlı Sistem Denetim Raporu Metodolojisi, Kalite Eşikleri ve Persona Tabanlı Analiz Şablonu.
- **[`docs/wiki/log.md`](./log.md)** — Projede yapılan mimari değişikliklerin, yeni özelliklerin (`INGEST`), incelemelerin (`REVIEW`) ve bakımların (`LINT`) kronolojik kayıt defteri.

---

## 🧩 2. Mevcut İş Modülleri (Features & Contracts)
- **Auth (`Application/Contracts/Identity/IAuthService` → `Infrastructure/Identity/AuthService`)** — JWT Access Token & 512-bit Refresh Token üretimi, Login, Register (`Registration:AllowPublic` ayarına bağlı), Revoke, Logout ve Şifre Sıfırlama akışları.
- **Users (`Application/Contracts/Identity/IUserService` → `Infrastructure/Identity/UserService`)** — Kullanıcı profil görüntüleme/güncelleme, parola/e-posta değiştirme, hesap silme (Soft-Delete) ve Admin rol yönetimi (`/api/v1/users/{id}/roles`) işlemleri.
- **Categories (`Core/ProjectApp.Application/Features/Categories`)** — Kategori CRUD işlemleri, ürünleriyle birlikte kategori sorgulama ve Mapster eşlemeleri (`ICategoryService` → `CategoryService`).
- **Products (`Core/ProjectApp.Application/Features/Products`)** — Ürün CRUD, stok güncelleme (`UpdateStock`), RowVersion ile iyimser eşzamanlılık, sayfalama ve `ProductAddedEvent` yayımlama (`IProductService` → `ProductService`).
- **Messaging (`Core/ProjectApp.Domain/Events`, `Application/Contracts/Infrastructure/IServiceBus` → `Infrastructure/Messaging`)** — `IEvent`/`IMessage` domain olayları, `System.Threading.Channels` tabanlı `InMemoryMessageQueue` + `InMemoryBusBackgroundService` ve `ProductAddedEventHandler` tüketici dinleyicisi.

---

## ⚙️ 3. Çekirdek ve Altyapı Bileşenleri
- **Domain (`Core/ProjectApp.Domain`)** — Sıfır dış bağımlılıklı saf POCO Entity'ler (`BaseEntity`, `AuditableEntity`, `IAuditEntity`) ve Domain Event'leri (`IEvent`, `IMessage`, `ProductAddedEvent`).
- **Infrastructure (`Infrastructure/ProjectApp.Infrastructure`)** — `AppDbContext` (`IdentityDbContext<AppUser, AppRole, Guid>`), `ProductRepository`, `CategoryRepository`, `UnitOfWork`, `AuditDbContextInterceptor`, `IdentityDataSeeder`, Identity servisleri (`AuthService`, `UserService`, `TokenService`, `JwtOption`), `EmailService` ve süreç içi mesajlaşma (`InMemoryServiceBus`, `InMemoryMessageQueue`, `InMemoryBusBackgroundService`, `ProductAddedEventHandler`).
- **Sunum (`Presentation/ProjectApp.WebAPI`)** — `CustomBaseController` (`api/v{version:apiVersion}/[controller]`, `/api/v1/...`), API versiyonlama (`VersioningExtensions`), `BearerSecuritySchemeTransformer` + Scalar UI (`/scalar/v1`), `RateLimiterExtensions`, `SecurityHeadersExtensions`, `ForwardedHeadersExtensions`, `ExceptionHandlers/` ve `Filters/`.

---

## 🧪 4. Testler
- **`tests/ProjectApp.UnitTests`** — Validator ve servis birim testleri (xUnit v3 + NSubstitute).
- **`tests/ProjectApp.IntegrationTests`** — `WebApplicationFactory` + SQLite in-memory uç nokta testleri.
- Komut: `dotnet test ProjectApp.slnx -v q`

---

## 📦 5. Reçeteler
- **[`recipes/README.md`](../../recipes/README.md)** — Seviye yükseltme kılavuzu: S4'ten S5'e (önbellek) geçiş ve opsiyonel Dapper tarifleri.

---

## 📄 6. Diğer
- **[`AGENTS.md`](../../AGENTS.md)** — Ajanlar için kısa kural özeti.
