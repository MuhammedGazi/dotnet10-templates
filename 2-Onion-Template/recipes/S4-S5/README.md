# Reçete: Onion S4 → S5 (ölçekli ürün: önbellek)

**Ön koşul:** S3-S4 reçetesi uygulanmış olmalı.
**Ne zaman:** Ölçülmüş okuma yükü (aynı veriyi saniyede çok kez okuyan uçlar), binlerce kullanıcı.
Önce ölç: yavaş sorguyu index/projeksiyonla düzeltmek önbellekten ucuzdur.

**Ne ekler (kod):**
- `ICacheService` + boyut sınırlı `MemoryCache` uygulaması (`Caching:SizeLimit`), `GetOrCreateAsync` ile
  cache stampede koruması (sabit 32 şeritli kilit: dinamik anahtarlarda kilit sözlüğü büyümez/sızmaz);
  `GetOrCreateOrNullAsync` ile negatif önbellek ("kayıt yok" sonucu kısa TTL, varsayılan 30 sn, saklanır)
- `CacheKeys` (`Products`, dinamik anahtar örneği `ProductById(Guid)`) ve `InvalidateProductsAsync()` uzantısı
- **Önbellek kapsamı yalnızca ürün listesidir (`GetAllListAsync`).** Kategori listesi, sayfalı ürün listesi ve
  `GetByIdAsync` önbellekli değildir; ihtiyaç olursa aynı kalıpla eklenir (ve geçersizleştirmesi de yazılır).
- **Geçersizleştirme kuralı:** ürünü (stok dahil) başka yoldan değiştiren kod (olay handler'ı, toplu iş, arka plan
  servisi) commit sonrası `cacheService.InvalidateProductsAsync()` çağırmalıdır; aksi halde liste önbelleğinde
  en fazla 5 dk bayat veri kalır. Şablondaki `ProductService`/`CategoryService` bunu zaten yapar.

**Kod içermeyen S5 notları (ihtiyaç olunca, ayrı karar ve dikkatli kod incelemesi ile):**
- **Çoklu instance:** in-memory önbellek her instance'ta ayrıdır; yatay ölçeklemede `HybridCache` + Redis.
- **Güvenilir olaylar:** in-memory bus yerine outbox tablosu + arka plan yayıncı veya dış kuyruk (RabbitMQ/Azure Service Bus).
  `MassTransit` şablonda yasaklıdır; gerekirse önce kullanıcıyla konuş.
- **Çok kiracılık:** `TenantId` + global query filter; `Finbuckle.MultiTenant` yasaklıdır.
- **Gözlemlenebilirlik:** OpenTelemetry (izleme + metrik), merkezi log.

## Adımlar
1. `git status` temiz; S4 seviyesindesin.
2. **Önce kontrol, sonra uygula** (`recete-uygula.ps1`; değiştirdiğin dosyaları birleştirir, üzerine yazmaz):
   ```powershell
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S4-S5          # yalnızca rapor
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S4-S5 -Uygula  # kopyalar ve birleştirir
   ```
   - KOPYALA: dosyaya dokunulmamış, kopyalanır. BIRLESIR: dosyayı sen de değiştirmişsin, değişiklikler çakışmıyor;
     betik `git merge-file` ile otomatik birleştirir (senin satırların korunur).
   - CAKISMA: sen ve reçete aynı satırlara dokunmuşsunuz; dosya DEĞİŞTİRİLMEZ (işaretli dosya yazılmaz). Bir tane bile
     CAKISMA varsa `-Uygula` hiçbir dosya yazmaz ve çıkış kodu 1 olur: betiğin verdiği `git diff --no-index` farkını
     elle aktar, commit'le, betiği yeniden çalıştır. VAR/EKSIK: dosyayı incele (betik ne yapılacağını yazar).
   - ELLE: `appsettings*.json` ve `CLAUDE.md` asla değiştirilmez (betik her biri için ne yapılacağını yazar). Önce değişikliklerini commit'le; sonuç `git diff` ile gözden geçirilir.
   - `recipes/S4-S5/degisiklikler.patch` yalnızca okumak için (reçetenin neyi değiştirdiğini gösterir).
3. Şema değişikliği yok: migration gerekmez. `appsettings.json`'a isteğe bağlı `"Caching": { "SizeLimit": 1024 }`.
4. Doğrula: build 0 uyarı, testler yeşil (şablon doğrulaması: 89 test; kendi testlerin bunun üstüne eklenir).
5. Proje `CLAUDE.md` → `Seviye: S5 (Onion)`; "Yapmayacaklarımız" listesini güncelle (önbellek artık var).

## KABUL
- Ürün listesi ikinci okumada veritabanına gitmez (log/profiler), ürün güncellemesinden sonra güncel veri döner.
- Ürünü servis dışı bir yoldan değiştiren her kod `InvalidateProductsAsync()` çağırır (yoksa liste bayat kalır).

## S5 tavanı: iki sunucuda ne bozulur

| Bileşen | Tek sunucu | 2+ sunucuda sorun | Çözüm/ne zaman |
| :--- | :--- | :--- | :--- |
| **Önbellek** | Süreç içi `IMemoryCache` hızlı ve sorunsuz çalışır. | Her sunucu kendi kopyasını tutar; bir sunucuda yapılan yazma diğerini geçersizleştirmez → bayat veri TTL (5 dk) boyunca kalır. | Dağıtık önbellek (`HybridCache` + Redis); yatay ölçeklemeye geçildiğinde. |
| **Olay kuyruğu** | Süreç içi `Channel` ile isteği alan sunucuda işlenir. | Olay yalnızca isteği alan sunucuda işlenir; uygulama yeniden başlatıldığında veya çöktüğünde kuyruktaki olaylar kaybolur. | Outbox tablosu + dış kuyruk (RabbitMQ / Azure Service Bus); olay kaybı kabul edilemiyorsa. |
| **Stampede kilidi** | 32 şeritli `SemaphoreSlim` ile tek sunucuda mükerrer DB sorguları engellenir. | Kilit sunucu başına yereldir; N sunucu aynı anda önbellek kaçırdığında DB'ye N kat yük biner. | Dağıtık kilit veya dağıtık önbellek altyapısı; yüksek trafikli çoklu instance ortamında. |
| **Rate limit** | Bölümlenmiş `FixedWindowLimiter` sayaçları bellek içinde tutulur. | Sayaçlar sunucu başına bağımsızdır; istemcinin efektif limiti N katına çıkar (yük dengeleyici round-robin dağıtırsa). | Dağıtık rate limiter (Redis tabanlı); katı hız sınırlandırma gerektiğinde. |
| **Oturum / Refresh token** | Refresh token ve security stamp veritabanında tutulur. | Sorun yok; tüm sunucular aynı veritabanına bakar. | Ek işlem gerekmez. |

### Somut Tetikleyiciler
- **İkinci sunucu eklenecekse:** Dağıtık önbellek (Redis / `HybridCache`) ve dağıtık rate limit.
- **Olay kaybı kabul edilemiyorsa:** Ödeme/stok gibi kritik işlemlerde outbox + dış kuyruk.
- **Birden fazla servis veya SLA varsa:** OpenTelemetry (izleme + metrik) ve merkezi log.
- **Kurumlar arası veri izolasyonu:** Çok kiracılık (`TenantId` + global query filter; tasarım S4'te yapılır).
