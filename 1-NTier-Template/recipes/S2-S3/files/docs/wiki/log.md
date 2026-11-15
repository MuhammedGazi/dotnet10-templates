# 📜 ProjectApp — Kronolojik Wiki Kayıt Defteri (Log)

> [!NOTE]
> Her önemli mimari değişiklik, yeni modül eklemesi (`INGEST`), kod incelemesi (`REVIEW`) veya dokümantasyon bakımı (`LINT`) bu dosyanın **en üstüne** `## [YYYY-AA-GG] [İŞLEM_TİPİ] | Kısa Açıklama` formatında eklenir.

---

## [Başlangıç] [INGEST] | Proje `dotnet new my-ntier` şablonundan oluşturuldu
- Hazır modüller: Auth (JWT + refresh token rotasyonu), Users, örnek Categories/Products, sağlık kontrolleri, rate limiting, Scalar UI ve xUnit testleri.
- İlk adımlar: `dotnet ef database update` ile şema ve roller kurulur; ayrıntı için `README.md` §8.
- İlk değişikliğinizi bu kaydın **üstüne** ekleyin.
