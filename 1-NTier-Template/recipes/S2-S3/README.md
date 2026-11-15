# Reçete: N-Tier S2 → S3 (kurum içi: denetim, soft delete, eşzamanlılık)

**Ön koşul:** S1-S2 reçetesi uygulanmış olmalı.
**Ne zaman:** 20+ kullanıcı veya 3+ rol; kayıtları kimin değiştirdiği önemli; aynı kaydı birden fazla kişi düzenliyor;
kullanıcılar kendi hesabını yönetecek (e-posta değiştirme, hesap silme).

**Ne ekler:**
- `AuditableEntity`: `CreatedBy`, `UpdatedBy`, `IsDeleted` + `RowVersion` (iyimser eşzamanlılık, 409)
- `AuditDbContextInterceptor`: kullanıcı bazlı denetim + otomatik soft delete; `AppDbContext` global soft delete filtresi
- Benzersiz alanlarda filtreli unique index (`[IsDeleted] = 0`)
- Kullanıcı self-servisi: `change-email` + `confirm-email-change`, `DELETE /api/users/account` (soft delete + anonimleştirme)
- Yönetici rol yönetimi: `GET /api/users/{id}/roles` ve `PUT /api/users/{id}/roles` (yalnızca `Admin` rolü; rol değişiminde eski token'lar geçersiz olur)
- Örnek stok ucu `PATCH /api/products/stock` (RowVersion zorunlu)
- `X-Correlation-Id` (istek kimliği, tüm log satırlarında)
- Sıkı Production açılış kontrolleri: boş `Cors:AllowedOrigins`, boş `ForwardedHeaders` ve `AllowedHosts: *` ile uygulama AÇILMAZ

**Frontend etkisi:** Ürün/kategori okuma yanıtlarında `rowVersion` alanı; güncelleme isteklerinde geri gönderilmeli (409'da yeniden oku).

## Adımlar
1. Ön koşul: `git status` temiz; S2 seviyesindesin.
2. **Önce kontrol, sonra uygula** (`recete-uygula.ps1`; değiştirdiğin dosyaları birleştirir, üzerine yazmaz):
   ```powershell
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S2-S3          # yalnızca rapor
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S2-S3 -Uygula  # kopyalar ve birleştirir
   ```
   - KOPYALA: dosyaya dokunulmamış, kopyalanır. BIRLESIR: dosyayı sen de değiştirmişsin, değişiklikler çakışmıyor;
     betik `git merge-file` ile otomatik birleştirir (senin satırların korunur).
   - CAKISMA: sen ve reçete aynı satırlara dokunmuşsunuz; dosya DEĞİŞTİRİLMEZ (işaretli dosya yazılmaz). Bir tane bile
     CAKISMA varsa `-Uygula` hiçbir dosya yazmaz ve çıkış kodu 1 olur: betiğin verdiği `git diff --no-index` farkını
     elle aktar, commit'le, betiği yeniden çalıştır. VAR/EKSIK: dosyayı incele (betik ne yapılacağını yazar).
   - ELLE: `appsettings*.json` ve `CLAUDE.md` asla değiştirilmez (betik her biri için ne yapılacağını yazar). Önce değişikliklerini commit'le; sonuç `git diff` ile gözden geçirilir.
   - `recipes/S2-S3/degisiklikler.patch` yalnızca okumak için (reçetenin neyi değiştirdiğini gösterir).
   - **ELLE adımı** `appsettings.Production.json`: yorum satırları S3 davranışını anlatır (S3'te eksik ayarla uygulama AÇILMAZ); reçetedeki
     yorumları kendi dosyana aktar. Canlı ortamda artık
     `Cors__AllowedOrigins__0`, `ForwardedHeaders__KnownProxies__0` (veya `NoProxy=true`) ve `AllowedHosts` VERİLMELİ.
3. `dotnet build ProjectApp.slnx`, sonra:
   `dotnet ef migrations add S3_DenetimVeSoftDelete --project ./ProjectApp.Repositories --startup-project ./ProjectApp.API`
   Migration'ı incele: mevcut verili tabloya `RowVersion` ve `IsDeleted` (varsayılan 0) eklenir; unique index'ler
   filtreliye döner. **Veritabanına uygulama kullanıcı onayıyla** (KRİTİK: veri içeren tabloda migration).
4. Kendi entity'lerin varsa: `AuditableEntity`'den türüyorlarsa otomatik kapsanır; configuration'larına
   `IsRowVersion()` ve filtreli unique index ekle (AGENTS.md "Yeni entity checklist").
5. Doğrula: build 0 uyarı, testler yeşil (şablon doğrulaması: 81 test; kendi testlerin bunun üstüne eklenir).
6. Belgeler: `files/docs/` altındaki wiki ve SYSTEM_AUDIT S3 sürecinin parçasıdır (kopyalandı); proje `CLAUDE.md` → `Seviye: S3 (N-Tier)`; "Yapmayacaklarımız" listesini güncelle (soft delete, RowVersion, audit artık var).

## KABUL
- Silinen ürün listede görünmez, aynı isimle yeniden oluşturulabilir.
- Eski `rowVersion` ile güncelleme 409.
- Production'da `Cors:AllowedOrigins` boşken uygulama açılmaz.
