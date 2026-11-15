# AGENTS.md — ProjectApp (N-Tier, .NET 10)

Ayrıntı için önce `README.md`, sonra `docs/AI_WORKFLOW.md` ve `recipes/README.md` okunur. Kod/identifier İngilizce; yorum ve kullanıcı mesajları Türkçe.

## Seviye
- Şablon **S1 (Vitrin)** seviyesindedir. Projenin gerçek seviyesi `CLAUDE.md` "Seviye" bölümündedir.
- Seviyenin üstündeki özellik (refresh token, soft delete, RowVersion, domain event, cache…) **eklenmez**; gerekiyorsa `recipes/` altındaki reçete önerilir ve kullanıcıya sorulur.
- Kodda olmayan bir özelliği "en iyi uygulama" diye geri ekleme.

## Katmanlar (tek yönlü bağımlılık)
- `ProjectApp.API` → `ProjectApp.Services` → `ProjectApp.Repositories`.
- Repositories: DbContext, entity, configuration, interceptor. DTO/iş kuralı yok. Repository sınıfı yok.
- Services: Feature-Service, DTO, validator, Mapster, `ServiceResult<T>`. `AppDbContext`'i doğrudan kullanır. MVC tiplerine bağımlı değil.
- API: Controller (`CustomBaseController`), filter, exception handler, pipeline, CORS/rate limit.

## Değişmez kurallar
- Servis metotları `ServiceResult`/`ServiceResult<T>` döner; iş hatası için exception fırlatılmaz.
- ID `Guid` (`Guid.CreateVersion7()`); mapping yalnızca Mapster; her async metotta `CancellationToken`.
- Kullanıcı kimliği yalnızca `ICurrentUserService.UserId`; endpoint `userId` parametresi almaz (IDOR).
- Zaman için `TimeProvider`; `DateTime.UtcNow` kullanılmaz.
- Yasaklı paketler: AutoMapper, MediatR, Swashbuckle, MassTransit, FluentValidation.AspNetCore, FluentAssertions.
- Paket sürümleri yalnızca `Directory.Packages.props` içinde.
- Güvenlik tabanı hiçbir seviyede düşmez: HTTPS, şifre hash (Identity), gizli anahtar repoda değil, girdi doğrulama, yedek.

## Yeni entity checklist
1. `AuditableEntity`'den türet (Created/Updated); configuration'da gerekiyorsa düz unique index; `DbSet` ekle.
2. Request/Response/Dto `record`, validator, `IRegister` mapping.
3. `I<Entity>Service` + servis (`AppDbContext` + `IMapper` enjekte) + `ServiceExtensions` kaydı; controller.
4. Unit (SQLite `TestDb`) + entegrasyon testi; `dotnet ef migrations add`; README güncelle.

## Doğrulama
- `dotnet build ProjectApp.slnx`
- `dotnet test ProjectApp.slnx -v q`

## Referans dosyalar
`README.md`, `docs/AI_WORKFLOW.md`, `recipes/README.md`, `Directory.Packages.props`, `ProjectApp.API/Program.cs`, `ProjectApp.Services/Features/Products/ProductService.cs`, `tests/ProjectApp.UnitTests/ProductServiceTests.cs`.
