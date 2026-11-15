# AGENTS.md — ProjectApp (N-Tier, .NET 10)

Ayrıntı için önce `README.md`, sonra `docs/AI_WORKFLOW.md` ve `docs/wiki/index.md` okunur. Kod/identifier İngilizce; yorum ve kullanıcı mesajları Türkçe.

## Seviye
- Proje **S3 (Kurum içi)** seviyesindedir. Projenin gerçek seviyesi `CLAUDE.md` "Seviye" bölümündedir. N-Tier mimarisinin tavanı S3'tür.
- Seviyenin üstündeki özellikler (domain event, API versiyonlama, önbellek → Onion S4-S5) **eklenmez**; bu özellikler Onion şablonundadır ve geçiş reçete değil yeni projeye taşımadır.
- Kodda olmayan bir özelliği "en iyi uygulama" diye geri ekleme.

## Katmanlar (tek yönlü bağımlılık)
- `ProjectApp.API` → `ProjectApp.Services` → `ProjectApp.Repositories`.
- Repositories: DbContext, entity, configuration, interceptor. DTO/iş kuralı yok. Repository sınıfı yok.
- Services: Feature-Service, DTO, validator, Mapster, `ServiceResult<T>`. `AppDbContext`'i doğrudan kullanır. MVC tiplerine bağımlı değil.
- API: Controller (`CustomBaseController`), filter, exception handler, pipeline, CORS/rate limit.

## Değişmez kurallar
- Servis metotları `ServiceResult`/`ServiceResult<T>` döner; iş hatası için exception fırlatılmaz.
- ID `Guid` (`Guid.CreateVersion7()`); mapping yalnızca Mapster; her async metotta `CancellationToken`.
- Kullanıcı kimliği yalnızca `ICurrentUserService.UserId`; endpoint `userId` parametresi almaz (IDOR). Rol kapsamı için `ICurrentUserService.IsInRole(role)`.
- Rol yönetimi: `ICurrentUserService.IsInRole(role)`; Admin uçları `GET`/`PUT /api/users/{id}/roles` (rol değişince eski token'lar 401). Yeni rol eklemek: `RoleNames` + `AppRoleConfiguration.HasData` + migration.
- Register `Registration:AllowPublic` ayarına bağlıdır: false (varsayılan) ise yalnızca Admin kullanıcı ekler; true ise herkes kayıt olur ve `User` rolü alır (açık kayıtta e-posta doğrulaması ayrıca eklenir).
- Zaman için `TimeProvider`; `DateTime.UtcNow` kullanılmaz.
- Yasaklı paketler: AutoMapper, MediatR, Swashbuckle, MassTransit, FluentValidation.AspNetCore, FluentAssertions.
- Paket sürümleri yalnızca `Directory.Packages.props` içinde.
- Güvenlik tabanı hiçbir seviyede düşmez: HTTPS, şifre hash (Identity), gizli anahtar repoda değil, girdi doğrulama, yedek.
- Sıkı Production açılış kontrolleri: `ConnectionStrings:DefaultConnection`, `Jwt:SigningKey`, `Cors:AllowedOrigins`, `AllowedHosts`, `ForwardedHeaders` yapılandırılmalıdır; eksik veya geçersizse uygulama açılmaz. `X-Correlation-Id` tüm loglarda yer alır.

## Yeni entity checklist
1. `AuditableEntity`'den türet (`Created`/`Updated` + `CreatedBy`/`UpdatedBy`, `IsDeleted` soft delete, `RowVersion`); configuration'da `Property(x => x.RowVersion).IsRowVersion()` ve filtreli unique index (`[IsDeleted] = 0`); `DbSet` ekle.
   - *Kritik RowVersion kontrolü (migration üretmeden ÖNCE):* configuration'da `Property(x => x.RowVersion).IsRowVersion()` mutlaka eklenmelidir. Unutulursa kolon `varbinary(max)` olur ve sonradan `AlterColumn` SQL Server'da 4927 hatası verir (DropColumn+AddColumn gerekir).
2. Request/Response/Dto `record`, validator, `IRegister` mapping. Update request + DTO'ya `byte[] RowVersion` ekle; validator'da zorunlu kıl (`NotEmpty()`).
3. `I<Entity>Service` + servis (`AppDbContext` + `IMapper` enjekte) + `ServiceExtensions` kaydı; controller. Serviste güncellemeden önce `context.Entry(entity).Property(x => x.RowVersion).OriginalValue = request.RowVersion;` ata (`DbUpdateConcurrencyException` → `GlobalExceptionHandler` ile 409 döner). Rol kapsamı için `ICurrentUserService.IsInRole(role)` kullan.
4. Unit (SQLite `TestDb`) + entegrasyon testi (kullanıcı gerekiyorsa `TestHelpers.RegisterAndLoginAsync`); `dotnet ef migrations add`; README güncelle.

## Doğrulama
- `dotnet build ProjectApp.slnx`
- `dotnet test ProjectApp.slnx -v q`

## Referans dosyalar
`README.md`, `docs/AI_WORKFLOW.md`, `docs/wiki/index.md`, `Directory.Packages.props`, `ProjectApp.API/Program.cs`, `ProjectApp.Services/Features/Products/ProductService.cs`, `tests/ProjectApp.UnitTests/ProductServiceTests.cs`, `tests/ProjectApp.IntegrationTests/TestHelpers.cs`.
