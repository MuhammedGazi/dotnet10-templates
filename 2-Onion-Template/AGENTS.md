# AGENTS.md — ProjectApp (Onion, .NET 10)

Ayrıntı için önce `README.md`, sonra `docs/AI_WORKFLOW.md`, `docs/wiki/index.md` ve `recipes/README.md` okunur. Kod/identifier İngilizce; yorum ve kullanıcı mesajları Türkçe.

## Seviye
- Şablon **S3 (Kurum içi)** seviyesindedir. Projenin gerçek seviyesi `CLAUDE.md` "Seviye" bölümündedir.
- Seviyenin üstündeki özellik (domain event/bus, API versiyonlama, önbellek, çok kiracılık, Dapper) **eklenmez**; gerekiyorsa `recipes/` altındaki reçete önerilir ve kullanıcıya sorulur.
- Kodda olmayan bir özelliği "en iyi uygulama" diye geri ekleme.

## Katmanlar (bağımlılık yönü içe doğru)
- Domain → hiçbir şey. Application → Domain. Infrastructure → Application.
- WebAPI → Application + Infrastructure.
- Application'a EF Core / ASP.NET Core paketi eklenmez; soyut sözleşmeler `Contracts/*` altındadır.
- Identity servisleri (`AuthService`, `UserService`, `TokenService`) Infrastructure/Identity altındadır.

## Değişmez kurallar
- Servis metotları `ServiceResult`/`ServiceResult<T>` (`Application.Common.Wrappers`) döner; iş hatası için exception yok.
- ID `Guid` (`Guid.CreateVersion7()`); mapping yalnızca Mapster; her async metotta `CancellationToken`.
- Kullanıcı kimliği yalnızca `ICurrentUserService.UserId`; endpoint `userId` parametresi almaz (IDOR).
- Register `Registration:AllowPublic` ayarına bağlıdır: false (varsayılan) ise yalnızca Admin kullanıcı ekler; true ise herkes kayıt olur ve `User` rolü alır (açık kayıtta e-posta doğrulaması ayrıca eklenir).
- Zaman için `TimeProvider`; route `api/...` (versiyonlama S4 reçetesiyle gelir).
- Yasaklı paketler: AutoMapper, MediatR, Swashbuckle, MassTransit, FluentValidation.AspNetCore, FluentAssertions.
- Paket sürümleri yalnızca `Directory.Packages.props` içinde.

## Yeni entity checklist
1. Domain'de `AuditableEntity`'den türet; configuration'da filtered unique index + `IsRowVersion()`.
2. `I<Entity>Repository` (Application) + repository (Infrastructure/Persistence) + `ServiceRegistration` kaydı; `DbSet` ekle.
3. Request/Dto `record`, validator, `IRegister` mapping (Application).
4. `I<Entity>Service` + servis + `AddApplicationServices` kaydı; controller.
5. Unit + entegrasyon testi; migration üret; README/wiki/log güncelle.

## Doğrulama
- `dotnet build ProjectApp.slnx`
- `dotnet test ProjectApp.slnx -v q`

## Referans dosyalar
`README.md`, `docs/AI_WORKFLOW.md`, `docs/SYSTEM_AUDIT.md`, `docs/wiki/index.md`, `docs/wiki/log.md`, `recipes/README.md`, `Directory.Packages.props`, `Presentation/ProjectApp.WebAPI/Program.cs`.
