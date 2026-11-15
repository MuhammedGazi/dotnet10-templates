# Arşiv: Generic Repository + Unit of Work + Dapper + NotFoundFilter (N-Tier)

> **Bu bir mekanik reçete DEĞİL, referans arşividir.** `files/` altındaki değişen dosyalar şablonun S3 durumuna
> göredir; S1/S2 projesine körlemesine kopyalanırsa o seviyenin dosyalarını bozar.

**Neden kaldırıldı:** EF Core `DbContext` zaten repository + unit of work'tür. Küçük/orta projede ek soyutlama
kod ve test yükü getiriyor. Şablonun her seviyesinde servisler
`AppDbContext`'i doğrudan kullanır.

**Ne zaman gerçekten gerekir:**
- **Dapper:** EF ile yazılamayan veya ölçülmüş performans sorunu olan salt okuma raporu (S4+). Örnek:
  `files/ProjectApp.Repositories/Products/ProductRepository.cs` → `GetTopPricedProductsWithDapperAsync`.
  Gereken: `Dapper` paketi (`Directory.Packages.props` + Repositories csproj), `DbContext.Database.GetDbConnection()`.
- **Generic repository / UoW:** Birden fazla veri kaynağı veya DbContext'i servislerden gizleme zorunluluğu. Genelde gerekmez.
- **NotFoundFilter:** `[ServiceFilter(typeof(NotFoundFilter<T, TId>))]` ile servise inmeden 404. Generic repository'ye bağlıdır.

**Nasıl kullanılır:** `files/` yalnızca okunur; ihtiyaç duyulan parça projeye uyarlanarak eklenir
(`degisiklikler.patch` S3 durumundaki farkı gösterir).
