[English](README.md) | Türkçe

# DotNet10Templates

Artımlı seviye yükseltme reçeteleriyle birlikte iki .NET 10 ASP.NET Core Web API `dotnet new` şablonu (N-Tier ve Onion Mimari).

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)

## ✨ İçindekiler

- **`1-NTier-Template` (`my-ntier`)** — S1 seviyesinden başlayan 3 projeli N-Tier Web API şablonu (EF Core, ASP.NET Core Identity + security-stamp iptalli JWT, FluentValidation, Mapster, Serilog, Scalar UI, xUnit).
- **`2-Onion-Template` (`my-onion`)** — S3 seviyesinden başlayan 4 projeli Onion Mimari Web API şablonu (Domain, Application, Infrastructure, WebAPI; refresh token rotasyonu, denetim alanları, soft delete, RowVersion, korelasyon kimliği, xUnit).
- **Seviye reçeteleri (`recipes/`)** — `recipes/recete-uygula.ps1` ile uygulanıp birleştirilen artımlı yükseltme paketleri (`S1-S2`, `S2-S3`, `S3-S4`, `S4-S5`) ve opsiyonel referans reçeteler.

> **Dil notu:** Şablon içi belgeler (şablon `README.md` dosyaları, `recipes/`, `AGENTS.md`, `docs/`) ve varsayılan hata/doğrulama mesajları Türkçedir.

## ⚙️ Önkoşullar

- **.NET 10 SDK**
- **SQL Server veya SQL Server LocalDB** — `Development` ortamında `IdentityDataSeeder` açılışta `MigrateAsync()` çalıştırır. `appsettings.json` içindeki varsayılan `ConnectionStrings:DefaultConnection` değeri `Server=(localdb)\mssqllocaldb;Database=ProjectAppDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` şeklindedir. Linux/macOS üzerinde `DefaultConnection` değerini yerel bir SQL Server konteynerine veya uzak sunucuya göre değiştirin.
- **PowerShell 5.1+ veya PowerShell 7 (`pwsh`)** — `recipes/recete-uygula.ps1` ve `tests/template-smoke.ps1` betikleri için gereklidir (Windows'ta `powershell -File` ile de `pwsh` ile de çalışır).

## 🚀 Hızlı başlangıç

Depoyu klonlayın ve iki şablonu kurun:

```powershell
git clone <repo-url>
cd DotNet10Templates-OSS
dotnet new install ./1-NTier-Template
dotnet new install ./2-Onion-Template
```

`my-ntier` veya `my-onion` ile yeni bir proje oluşturun (ilk restore adımını atlamak için `--skip-restore` ekleyebilirsiniz):

```powershell
dotnet new my-ntier -n MyApp -o ./MyApp
# veya
dotnet new my-onion -n MyApp -o ./MyApp
```

Oluşturulan çözüm kökünden derleyin, testleri çalıştırın ve API'yi başlatın:

```powershell
cd MyApp
dotnet build MyApp.slnx
dotnet test --solution MyApp.slnx --no-build
dotnet run --project ./MyApp.API   # my-onion için ./Presentation/MyApp.WebAPI
```

> `Development` ortamında `IdentityDataSeeder` otomatik olarak `MigrateAsync()` çalıştırır ve `appsettings.Development.json` içindeki `Seed:AdminPassword` ile varsayılan yönetici hesabını (`admin@admin.com`) oluşturur. Scalar UI `/scalar/v1` adresinde açılır.

## 🧭 Hangi şablon?

| Şablon | Kısa ad | Katmanlar | Başlangıç seviyesi | Yükseltme seviyeleri | Ne zaman seçilir? |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`1-NTier-Template`** | `my-ntier` | 3 (`API` → `Services` → `Repositories`) | S1 (Vitrin) | S2, S3 (`recipes/` ile) | Servislerin `AppDbContext`'i doğrudan kullandığı vitrin siteleri, admin panelleri ve küçük ekip API'leri. |
| **`2-Onion-Template`** | `my-onion` | 4 (`WebAPI` → `Application` / `Infrastructure` → `Domain`) | S3 (Kurum içi) | S4, S5 (`recipes/` ile) | Bağımsız domain, refresh token, denetim alanları, soft delete ve `RowVersion` gereken kurum içi veya çok rollü uygulamalar. |

## 📶 Seviyeler ve reçeteler

Özellikler proje seviyesine göre ayrılmıştır; ihtiyaç doğana kadar şablonlar sade kalır:

- **S1 — Vitrin (`my-ntier` tabanı):** 1–3 admin, anonim ziyaretçi; Identity çekirdeği, `Feature-Service`, `ServiceResult<T>`, global rate limit, security-stamp ile token iptali.
- **S2 — Küçük ekip (`S1-S2` reçetesi):** 3–20 kullanıcı, 1–2 rol; kayıt varsayılan olarak kapalıdır (yalnızca Admin kullanıcı ekler; `Registration:AllowPublic` `true` yapıldığında herkese açık `User` kaydı açılır), hash'li refresh token rotasyonu, çıkış, şifre sıfırlama, profil, auth rate limit.
- **S3 — Kurum içi (`S2-S3` reçetesi / `my-onion` tabanı):** 20–300 kullanıcı, 3–5 rol; denetim alanları, soft delete, `RowVersion` (`409 Conflict`), e-posta değiştirme, hesap silme, korelasyon kimliği.
- **S4 — Ürün (`S3-S4` reçetesi):** 300–5000 kullanıcı; in-memory bus ile domain event'ler ve API versiyonlama (`/api/v1`).
- **S5 — Ölçekli ürün (`S4-S5` reçetesi):** Binlerce kullanıcı; önbellek (`ICacheService`) ve Redis, outbox, çok kiracılık mimari notları.

Reçete betiğini **oluşturulan projenin** kökünden çalıştırın. `-Uygula` temiz bir git ağacı ister (önce `git init` ve ilk commit gerekir). Önce kuru çalıştırma ile rapor alın, ardından `-Uygula` ile uygulayın (dokunulmamış dosyalar kopyalanır, değiştirilen dosyalar `git merge-file` ile birleştirilir); son olarak ilgili reçetenin `README.md` dosyasındaki elle adımları (`appsettings*.json`, `CLAUDE.md`) ve varsa EF Core migration adımını tamamlayın:

```powershell
pwsh -File recipes/recete-uygula.ps1 -Recete S1-S2
pwsh -File recipes/recete-uygula.ps1 -Recete S1-S2 -Uygula
```

Tam seviye matrisi için [docs/SEVIYE_TANIMLARI.md](docs/SEVIYE_TANIMLARI.md) belgesine bakın.

## 🗂️ Depo yapısı

- [`1-NTier-Template/`](1-NTier-Template/README.md) — N-Tier `.NET 10` şablonu (`my-ntier`) ve `recipes/` (`S1-S2`, `S2-S3`, `opsiyonel-generic-repository-dapper`).
- [`2-Onion-Template/`](2-Onion-Template/README.md) — Onion Mimari `.NET 10` şablonu (`my-onion`) ve `recipes/` (`S3-S4`, `S4-S5`, `opsiyonel-dapper`).
- [`tests/`](tests/template-smoke.ps1) — Şablon üretim smoke test betiği (`template-smoke.ps1`).
- [`tools/`](tools/recete/README.md) — Reçete bakım betikleri (`gen_recipe.py`, `patch_regen.py`, `add_onceki.py`, `verify.sh`).
- [`docs/`](docs/SEVIYE_TANIMLARI.md) — Seviye tanımları (`SEVIYE_TANIMLARI.md`).

## 🧪 Testler

Her iki şablon da xUnit birim ve entegrasyon testleri içerir (`WebApplicationFactory` + SQLite in-memory; SQL Server gerekmez). `global.json` dosyası `Microsoft.Testing.Platform` ayarladığı için `dotnet test` komutunu şablon çözüm kökünde çalıştırın:

```powershell
dotnet build ProjectApp.slnx
dotnet test --solution ProjectApp.slnx --no-build
```

Her iki şablondan proje üretimini, `ProjectApp` ad değişimini, derlemeyi ve testleri doğrulamak için (önce her iki şablonun `dotnet new install` ile kurulu olması gerekir):

```powershell
pwsh ./tests/template-smoke.ps1
```

## 🤖 Yapay zeka destekli geliştirme

Her iki şablonda da yapay zeka kodlama asistanlarına mimari sınırları, yasaklı paket kurallarını ve iş akışını aktaran `AGENTS.md`, `CLAUDE.md` ve `docs/AI_WORKFLOW.md` dosyaları bulunur. Bunları kullanmak tamamen isteğe bağlıdır; şablonların derlenmesini veya çalışmasını etkilemez.

---

🤝 Katkı → [CONTRIBUTING.md](CONTRIBUTING.md) · 📄 Lisans → MIT ([LICENSE](LICENSE))
