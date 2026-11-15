# Reçeteler (N-Tier)

Bu şablon **S1 (Vitrin)** seviyesinden başlar. Seviye atlamak sıfırdan kod yazmakla değil, buradaki reçetelerle yapılır.
Seviye tanımları ve "ne zaman" kriterleri: proje `CLAUDE.md` "Seviye" bölümü ve şablon deposundaki `docs/SEVIYE_TANIMLARI.md`.

| Reçete | Seviye | Özet |
|---|---|---|
| [`S1-S2`](S1-S2/README.md) | S1 → S2 | Admin'in açtığı kullanıcılar, refresh token, logout, şifre sıfırlama, profil, auth rate limit |
| [`S2-S3`](S2-S3/README.md) | S2 → S3 | Denetim alanları, soft delete, RowVersion/409, e-posta değiştirme, hesap silme, korelasyon kimliği, sıkı prod kontrolleri, wiki + SYSTEM_AUDIT |
| [`opsiyonel-generic-repository-dapper`](opsiyonel-generic-repository-dapper/README.md) | arşiv | Generic repo/UoW, Dapper, NotFoundFilter: referans kod, mekanik uygulanmaz |

S3'ün ötesi (domain event, API versiyonlama, önbellek, çok kurum) Onion şablonundadır (`my-onion`, S3-S5).
N-Tier S3'ten Onion'a geçiş bir reçete değil, yeni projeye taşımadır.

Her reçete klasörü:
- `README.md`: ne zaman, ne ekler, adımlar, KABUL
- `files/`: eklenen ve değişen dosyaların hedef seviyedeki tam hali (proje köküne göre yollar)
- `dosyalar.txt`: A (yeni) / M (değişen) / D (silinen) listesi
- `silinecekler.txt`: silinecek dosyalar
- `onceki/`: değişen (M) dosyaların önceki seviyedeki orijinal hali; `recete-uygula.ps1` projedeki dosyayı bununla karşılaştırır
- `degisiklikler.patch`: değişen dosyaların farkı (özelleştirilmiş projede elle birleştirmek için)

Uygulama: `recete-uygula.ps1 -Recete <ad>` önce rapor verir; dokunulmamış dosyaları `-Uygula` ile kopyalar, senin değiştirdiğin
dosyaları `git merge-file` ile otomatik birleştirir. Böylece projeye eklediğin kod reçete uygulanırken kaybolmaz.
`-Uygula` temiz git ağacı ister; tek bir çakışma bile varsa hiçbir dosya yazılmaz (çıkış kodu 1) (`git diff --no-index` ile elle aktar); `CLAUDE.md` ve `appsettings*.json` elle güncellenir.
- Migration dosyası yoktur; reçete `dotnet ef migrations add` komutunu verir.
