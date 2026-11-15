# Reçete: Onion S3 → S4 (ürün: domain event + API versiyonlama)

**Ne zaman:** 300+ kullanıcı veya birden fazla kurum; birden fazla istemci (web + mobil + dış entegrasyon)
API'ye bağlanacak ve sözleşme kırılmadan değişmeli; bir işlemin yan etkileri (bildirim, entegrasyon) ana akıştan ayrılmalı.

**Ne ekler:**
- Domain event altyapısı: `IEvent`/`IMessage`, `IServiceBus`, `IEventHandler<T>`, `System.Threading.Channels` tabanlı
  in-memory kuyruk + `BackgroundService` tüketici; örnek `ProductAddedEvent` ve handler
- API versiyonlama (`Asp.Versioning`): rotalar `/api/v1/...`; `CustomBaseController` `[ApiVersion("1.0")]`

**Frontend etkisi (KIRICI):** tüm rotalar `/api/...` → `/api/v1/...`. Frontend `environment` taban adresini güncelle.
**Bilinmesi gereken:** in-memory bus süreç içidir; uygulama yeniden başlarsa kuyruktaki olaylar kaybolur.
Kayıp kabul edilemiyorsa S5 notlarına bak (outbox / dış kuyruk).

## Adımlar
1. `git status` temiz; proje Onion S3 şablonundan.
2. **Önce kontrol, sonra uygula** (`recete-uygula.ps1`; değiştirdiğin dosyaları birleştirir, üzerine yazmaz):
   ```powershell
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S3-S4          # yalnızca rapor
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S3-S4 -Uygula  # kopyalar ve birleştirir
   ```
   - KOPYALA: dosyaya dokunulmamış, kopyalanır. BIRLESIR: dosyayı sen de değiştirmişsin, değişiklikler çakışmıyor;
     betik `git merge-file` ile otomatik birleştirir (senin satırların korunur).
   - CAKISMA: sen ve reçete aynı satırlara dokunmuşsunuz; dosya DEĞİŞTİRİLMEZ (işaretli dosya yazılmaz). Bir tane bile
     CAKISMA varsa `-Uygula` hiçbir dosya yazmaz ve çıkış kodu 1 olur: betiğin verdiği `git diff --no-index` farkını
     elle aktar, commit'le, betiği yeniden çalıştır. VAR/EKSIK: dosyayı incele (betik ne yapılacağını yazar).
   - ELLE: `appsettings*.json` ve `CLAUDE.md` asla değiştirilmez (betik her biri için ne yapılacağını yazar). Önce değişikliklerini commit'le; sonuç `git diff` ile gözden geçirilir.
   - `recipes/S3-S4/degisiklikler.patch` yalnızca okumak için (reçetenin neyi değiştirdiğini gösterir).
3. Kendi controller ve testlerinde `/api/` rota öneklerini `/api/v1/` yap; servislerdeki `SuccessAsCreated` konum yolları da.
4. Şema değişikliği yok: migration gerekmez.
5. Doğrula: `dotnet build ProjectApp.slnx` (0 uyarı), `dotnet test ProjectApp.slnx` (şablon doğrulaması: 80 test; kendi testlerin bunun üstüne eklenir).
6. Proje `CLAUDE.md` → `Seviye: S4 (Onion)`; "Yapmayacaklarımız" listesini güncelle (domain event, API versiyonlama artık var).

## KABUL
- `/api/v1/products` 200, `/api/products` 404.
- Ürün oluşturulunca `ProductAddedEventHandler` log satırı yazılır.
