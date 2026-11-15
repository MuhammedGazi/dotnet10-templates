# Reçeteler (Onion)

Bu şablon **S3 (Kurum içi)** seviyesinden başlar: 4 proje, kullanıcı oturumları (refresh token, security stamp),
denetim alanları, soft delete, RowVersion, `Registration:AllowPublic` ayarıyla açılıp kapanan register. S1-S2 için `my-ntier` şablonu kullanılır.
Seviye atlamak sıfırdan kod yazmakla değil, buradaki reçetelerle yapılır.

| Reçete | Seviye | Özet |
|---|---|---|
| [`S3-S4`](S3-S4/README.md) | S3 → S4 | Domain event + in-memory bus, API versiyonlama (`/api/v1`) |
| [`S4-S5`](S4-S5/README.md) | S4 → S5 | Önbellek (`ICacheService`); Redis, outbox, çok kiracılık için notlar |
| [`opsiyonel-dapper`](opsiyonel-dapper/README.md) | arşiv | Dapper raporlama: referans kod, mekanik uygulanmaz |

Her reçete klasörü: `README.md` (ne zaman, ne ekler, adımlar, KABUL), `files/` (hedef seviyedeki tam dosyalar),
`dosyalar.txt` (A/M/D), `silinecekler.txt`, `onceki/` (değişen dosyaların orijinal hali), `degisiklikler.patch` (özelleştirilmiş
projede elle birleştirmek için). Uygulama `recete-uygula.ps1 -Recete <ad>` ile: önce rapor, sonra `-Uygula`; değiştirdiğin
dosyalar `git merge-file` ile otomatik birleştirilir. `-Uygula` temiz git ağacı ister; tek bir çakışma bile varsa hiçbir dosya yazılmaz (çıkış kodu 1)
(`git diff --no-index` ile elle aktar); `CLAUDE.md` ve `appsettings*.json` elle güncellenir.
