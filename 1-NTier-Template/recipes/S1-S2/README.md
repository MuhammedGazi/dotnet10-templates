# Reçete: N-Tier S1 → S2 (küçük ekip: kullanıcı oturumları)

**Ne zaman:** Giriş yapan kullanıcı sayısı 3'ü geçti veya admin dışında bir rol (ör. Personel) giriş yapacak.
Seviye tanımı: proje `CLAUDE.md` "Seviye" bölümü (şablon deposundaki `docs/SEVIYE_TANIMLARI.md` esas alınarak doldurulur).

**Ne ekler:**
- Kullanıcı kaydı: `POST /api/auth/register`. `Registration:AllowPublic` false (varsayılan) ise yalnızca Admin
  kullanıcı ekler, true ise herkes kayıt olur; yeni kullanıcı her zaman `User` rolü alır.
  **Açık kayıt (true) seçersen:** login `EmailConfirmed`'a bakmaz; e-posta doğrulamasını ayrıca ekle
- Refresh token (SHA-256 hash'li saklama, rotasyon), `refresh-token`, `revoke-token`, `logout`
- Şifremi unuttum / sıfırlama: `forgot-password`, `reset-password` (`IEmailService` ile; canlıda gerçek SMTP gerekir)
- Profil: `GET /api/users/me`, `PUT /api/users/profile`
- Kimlik uçlarına sıkı rate limit (`auth-policy`) ve uca özel `fixed` politikası
- Access token süresi 120 → 30 dakika (refresh token ile yenilenir)

**Frontend etkisi:** Login yanıtı artık `refreshToken` ve `refreshTokenExpiry` içerir; 401'de `refresh-token` çağrılır.

## Adımlar (her adım mekanik)

1. Ön koşul: `git status` temiz. Proje S1 şablonundan üretilmiş olmalı (bu klasörün yanında `recipes/S1-S2` var).
2. **Önce kontrol, sonra uygula** (`recete-uygula.ps1`; değiştirdiğin dosyaları birleştirir, üzerine yazmaz):
   ```powershell
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S1-S2          # yalnızca rapor
   powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S1-S2 -Uygula  # kopyalar ve birleştirir
   ```
   - KOPYALA: dosyaya dokunulmamış, kopyalanır. BIRLESIR: dosyayı sen de değiştirmişsin, değişiklikler çakışmıyor;
     betik `git merge-file` ile otomatik birleştirir (senin satırların korunur).
   - CAKISMA: sen ve reçete aynı satırlara dokunmuşsunuz; dosya DEĞİŞTİRİLMEZ (işaretli dosya yazılmaz). Bir tane bile
     CAKISMA varsa `-Uygula` hiçbir dosya yazmaz ve çıkış kodu 1 olur: betiğin verdiği `git diff --no-index` farkını
     elle aktar, commit'le, betiği yeniden çalıştır. VAR/EKSIK: dosyayı incele (betik ne yapılacağını yazar).
   - ELLE: `appsettings*.json` ve `CLAUDE.md` asla değiştirilmez (betik her biri için ne yapılacağını yazar). Önce değişikliklerini commit'le; sonuç `git diff` ile gözden geçirilir.
   - `recipes/S1-S2/degisiklikler.patch` yalnızca okumak için (reçetenin neyi değiştirdiğini gösterir).
   - **`appsettings*.json` anahtarları** (bağlantı dizesi vb. projeye özgü olduğu için elle):
     - `appsettings.json` → `Jwt.AccessTokenMinutes: 30`, `Jwt.RefreshTokenDays: 7`, `RateLimiting.Auth.PermitLimit: 15`, `Registration.AllowPublic: false` (açık kayıt istersen `true` + e-posta doğrulaması);
       S1'den kalan `_AccessTokenMinutes_comment` satırını sil (artık refresh token var)
     - `appsettings.Production.json` → `Jwt.AccessTokenMinutes: 15`, `Jwt.RefreshTokenDays: 7`, `RateLimiting.Auth.PermitLimit: 15`
3. `silinecekler.txt` doluysa listelenen dosyaları sil (bu reçetede boş).
4. Derle, sonra migration üret (veritabanına UYGULAMA kullanıcı onayıyla):
   ```powershell
   dotnet build ProjectApp.slnx
   dotnet ef migrations add S2_KullaniciOturumlari --project ./ProjectApp.Repositories --startup-project ./ProjectApp.API
   ```
5. Doğrula: `dotnet build ProjectApp.slnx` (0 hata, 0 uyarı) ve `dotnet test ProjectApp.slnx` (yeşil).
6. Proje `CLAUDE.md` → `Seviye: S2 (N-Tier)`; "Yapmayacaklarımız" listesini güncelle.
7. README/AGENTS'ta kimlik uçları tablosunu güncelle (şablon deposundaki S2 tarifi: bu reçetenin uçları).

## KABUL
- Build 0 uyarı, testler yeşil (şablon doğrulaması: S1 45 test → S1+S2 58 test).
- `POST /api/auth/register`: varsayılan ayarda anonim 401, User rolü 403, Admin 201 (yeni kullanıcı yalnızca `User`); `Registration:AllowPublic=true` iken anonim 201.
- Logout sonrası eski access token 401.

## Doğrulama kaydı
Bu reçete, şablonun S1 durumunun temiz kopyasına uygulanıp build + migration + test ile doğrulandı (10 Kasım 2026).
