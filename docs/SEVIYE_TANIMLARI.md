# Seviye Tanımları

Bu belge, `1-NTier-Template` (`my-ntier`) ve `2-Onion-Template` (`my-onion`) şablonlarında projenin büyüklüğüne göre hangi mimari ve altyapı özelliklerinin eklendiğini tanımlar. Şablonlar başlangıçta gereksiz karmaşıklıktan kaçınmak için en sade çalışır çekirdekle başlar; ihtiyaç doğdukça artımlı reçetelerle üst seviyelere geçilir.

## Seviye matrisi

Tanımlar eklemelidir: her üst seviye bir önceki seviyenin üzerine yalnızca ilgili reçeteyle gelen özellikleri ekler.

| Seviye | Kime uygun | Destekleyen şablon(lar) | Geçiş reçetesi | Bu seviyede eklenenler |
| :--- | :--- | :--- | :--- | :--- |
| **S1 — Vitrin** | 1–3 admin, anonim ziyaretçiler | `1-NTier-Template` (`my-ntier`) | Taban (doğrudan şablon) | 3 katmanlı N-Tier (`API` → `Services` → `Repositories`; servisler `AppDbContext`'i doğrudan kullanır), `Feature-Service` ve `ServiceResult<T>`, Identity çekirdeği (admin girişi, şifre değiştirme, lockout), security stamp ile token iptali (120 dk access token, refresh token yok), tek global rate limit, `Created`/`Updated` zaman damgaları, örnek `Products` / `Categories` modülü, Scalar UI, xUnit testleri. |
| **S2 — Küçük ekip** | 3–20 kullanıcı, 1–2 rol | `1-NTier-Template` (`my-ntier`) | `S1-S2` | `POST /api/auth/register` (`Registration:AllowPublic` varsayılan `false` = yalnızca Admin ekler; `true` ile herkese açık `User` kaydı), SHA-256 hash'li refresh token rotasyonu (`refresh-token`, `revoke-token`, `logout`; access token 30 dk), `forgot-password` / `reset-password` (`IEmailService`), profil uçları (`/api/users/me`, `profile`), kimlik uçlarına `auth-policy` rate limit. |
| **S3 — Kurum içi** | 20–300 kullanıcı, 3–5 rol | `1-NTier-Template` (`my-ntier`) veya `2-Onion-Template` (`my-onion` tabanı) | `S2-S3` (N-Tier) veya taban (`my-onion`) | `AuditableEntity` (`CreatedBy`, `UpdatedBy`, `IsDeleted`, `RowVersion` / `409 Conflict`), global soft delete filtresi ve filtreli unique index'ler, self-servis `change-email` / `confirm-email-change` ve `DELETE /api/users/account`, Admin rol yönetimi uçları (`GET`/`PUT /api/users/{id}/roles`), `X-Correlation-Id` izleme, sıkı Production açılış kontrolleri (boş CORS / `ForwardedHeaders` / `AllowedHosts: *` engeli), `docs/wiki` ve `SYSTEM_AUDIT.md`. `my-onion` ayrıca 4 projeli Onion mimarisi (`Domain`, `Application`, `Infrastructure`, `WebAPI`) ve varlığa özel repository + `IUnitOfWork` içerir. |
| **S4 — Ürün** | 300–5000 kullanıcı, çoklu istemci | `2-Onion-Template` (`my-onion`) | `S3-S4` | Domain event altyapısı (`IEvent`/`IMessage`, `IServiceBus`, `IEventHandler<T>`, `System.Threading.Channels` tabanlı süreç içi kuyruk + `BackgroundService` tüketici, örnek `ProductAddedEvent`) ve `Asp.Versioning` ile API versiyonlama (`/api/v1/...`). |
| **S5 — Ölçekli ürün** | Binlerce kullanıcı, yüksek okuma yükü | `2-Onion-Template` (`my-onion`) | `S4-S5` | `ICacheService` ve boyut sınırlı `MemoryCache` (`Caching:SizeLimit`), 32 şeritli stampede kilidi (`GetOrCreateAsync`), negatif önbellekleme (`GetOrCreateOrNullAsync`) ve ürün listesi önbellek geçersizleştirmesi (`InvalidateProductsAsync`). (Çoklu sunucu için Redis/outbox/çok kiracılık yalnızca mimari not olarak yer alır.) |

> N-Tier S3'ten Onion S3'e geçiş bir reçete değildir; Onion mimarisi gerekiyorsa `my-onion` tabanından başlanır. Her iki şablonda ayrıca isteğe bağlı referans kod içeren `opsiyonel-*` reçete klasörleri bulunur; bunlar mekanik olarak uygulanmaz.

## Dahil olmayanlar

Aşağıdaki özellikler ve altyapılar şablonlar veya reçeteler tarafından **sağlanmaz**; projenizin ihtiyacına göre ayrıca tasarlamanız ve eklemeniz gerekir:

- Ödeme altyapısı (sanal POS, idempotency, webhook doğrulama)
- Hassas kişisel veri / KVKK uyum mekanizmaları (alan düzeyinde şifreleme, erişim logu, otomatik anonimleştirme iş akışları)
- Dış sistem entegrasyon katmanları (HTTP istemci retry/timeout politikaları, circuit breaker)
- Dosya ve görsel depolama (yerel disk, S3, Azure Blob vb.)
- Merkezi audit log tablosu (mevcut `AuditableEntity` yalnızca son oluşturan/güncelleyen ve yumuşak silme bilgisini tutar)
- İki faktörlü kimlik doğrulama (2FA / MFA)
- Dağıtık rate limiting ve dağıtık önbellek (Redis / `HybridCache`)
- Çok kiracılık (`TenantId` izolasyonu), outbox tablosu / dış mesaj kuyruğu ve OpenTelemetry izleme altyapısı

## Reçetelerin uygulanması

Reçeteler, `dotnet new` ile oluşturduğunuz projenin kök dizininden `recipes/recete-uygula.ps1` betiğiyle uygulanır. Betik önce parametre olarak yalnızca `-Recete <ad>` verilerek kuru çalıştırma (dry-run) modunda rapor alır; ardından temiz bir git çalışma ağacında (`git init` + ilk commit sonrası) `-Uygula` bayrağıyla dokunulmamış dosyaları kopyalar ve projenizde değiştirdiğiniz dosyaları `git merge-file` ile otomatik birleştirir. `appsettings*.json` ve `CLAUDE.md` dosyalarına betik dokunmaz; bu dosyalar ile gerekiyorsa EF Core migration adımları ilgili reçetenin `README.md` yönergelerine göre elle tamamlanır.

Ayrıntılar için şablonların reçete dizinlerine bakın:
- N-Tier reçeteleri: [`1-NTier-Template/recipes/README.md`](../1-NTier-Template/recipes/README.md)
- Onion reçeteleri: [`2-Onion-Template/recipes/README.md`](../2-Onion-Template/recipes/README.md)
