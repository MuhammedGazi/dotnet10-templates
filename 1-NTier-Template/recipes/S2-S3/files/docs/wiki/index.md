# 🗺️ ProjectApp — Proje Wiki İçerik Haritası (Index)

> [!NOTE]
> Bu dosya, projedeki tüm mimari kararların, kuralların ve modüllerin merkezi haritasıdır. AI ajanları kod yazmadan önce **[`README.md`](../../README.md)** ile birlikte bu indeks dosyasını okumak ve yaptığı değişiklikleri buraya işlemekle yükümlüdür.

---

## 🏛️ 1. Temel Anayasa ve Metodoloji Belgeleri
- **[`README.md`](../../README.md)** — `.NET 10` N-Tier Mimari Anayasası, Katman Kuralları, Yasaklı Paketler ve Yeni Özellik Ekleme Rehberi.
- **[`docs/AI_WORKFLOW.md`](../AI_WORKFLOW.md)** — AI Destekli Geliştirme İş Akışı, Wiki Yönetimi, Kod İnceleme, Plan Modu, Doğrulama ve Commit Kuralları.
- **[`docs/SYSTEM_AUDIT.md`](../SYSTEM_AUDIT.md)** — Kapsamlı Sistem Denetim Raporu Metodolojisi, Kalite Eşikleri ve Persona Tabanlı Analiz Şablonu.
- **[`docs/wiki/log.md`](./log.md)** — Projede yapılan mimari değişikliklerin, yeni özelliklerin (`INGEST`), incelemelerin (`REVIEW`) ve bakımların (`LINT`) kronolojik kayıt defteri.

---

## 🧩 2. Mevcut İş Modülleri (Features)
- **Auth (`ProjectApp.Services/Features/Auth`)** — JWT Access Token (`security_stamp` doğrulamalı) & 512-bit Refresh Token üretimi, Login, Register, Revoke, Logout ve Şifre Sıfırlama akışları (`IAuthService` → `AuthService`).
- **Users (`ProjectApp.Services/Features/Users`)** — Kullanıcı profil görüntüleme/güncelleme, parola değiştirme (lockout korumalı), self-servis e-posta değiştirme, hesap silme (Soft-Delete + anonimleştirme) ve yönetici rol yönetimi (`/roles`) işlemleri (`IUserService` → `UserService`).
- **Categories (`ProjectApp.Services/Features/Categories`)** — Kategori CRUD işlemleri, ürünleriyle birlikte kategori sorgulama ve Mapster eşlemeleri (`ICategoryService` → `CategoryService`).
- **Products (`ProjectApp.Services/Features/Products`)** — Ürün CRUD, stok güncelleme (`UpdateStock`), RowVersion ile iyimser eşzamanlılık (`Update` ve `UpdateStock`) ve veritabanı seviyesinde sayfalama (`IProductService` → `ProductService`).

---

## ⚙️ 3. Çekirdek Altyapı Bileşenleri
- **Veri Erişimi (`ProjectApp.Repositories`)** — `AppDbContext` (`IdentityDbContext<AppUser, AppRole, Guid>`), doğrudan DbContext kullanımı, global query filter (`IsDeleted`) ve `AuditDbContextInterceptor` (otomatik `Created`/`Updated`/`CreatedBy`/`UpdatedBy` ve Soft-Delete `IsDeleted` yönetimi).
- **Sunum ve Güvenlik (`ProjectApp.API`)** — `CustomBaseController`, `BearerSecuritySchemeTransformer` + Scalar UI (`/scalar/v1`), `RateLimiterExtensions` (global limiter, `auth-policy`, bölümlenmiş `fixed` politikası), `SecurityHeadersExtensions`, `ForwardedHeadersExtensions` (`KnownProxies`/`KnownNetworks`/`NoProxy` + `ValidateAllowedHosts`), `CorrelationIdExtensions` (`X-Correlation-Id`), `ExceptionHandlers/`, `Filters/` ve `IdentityDataSeeder`.

---

## 🧪 4. Testler
- **`tests/ProjectApp.UnitTests`** — Validator ve servis birim testleri (xUnit v3 + SQLite in-memory `TestDb`).
- **`tests/ProjectApp.IntegrationTests`** — `WebApplicationFactory` + SQLite in-memory uç nokta testleri (`AuthEndpointsTests`, `CatalogEndpointsTests`, `SecurityEndpointsTests`).
- Komut: `dotnet test ProjectApp.slnx -v q`

---

## 📄 5. Diğer
- **[`AGENTS.md`](../../AGENTS.md)** — Ajanlar için kısa kural özeti.
