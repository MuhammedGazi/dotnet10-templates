# Arşiv: Dapper ile raporlama (Onion)

> **Mekanik reçete DEĞİL, referans arşividir.** `files/` altındaki değişen dosyalar şablonun eski tam (S5) durumuna
> göredir; körlemesine kopyalanmaz.

**Neden kaldırıldı:** EF Core + Dapper birlikte küçük/orta projede gereksiz ikinci veri erişim yolu.
**Ne zaman gerekir:** EF ile yazılamayan veya ölçülmüş performans sorunu olan salt okuma raporu.
**Parçalar:** `ISqlConnectionFactory` + `SqlConnectionFactory` (Persistence), `ProductRepository.GetTopPricedProductsWithDapperAsync`,
`Dapper` paketi (`Directory.Packages.props` + Persistence csproj), `GET /api/v1/products/top-priced`.
**Kullanım:** ihtiyaç duyulan parça projeye uyarlanarak eklenir (`degisiklikler.patch` farkı gösterir).

Not: Bu arşivdeki yollar eski 5 projeli yapıya göredir; güncel yapıda Persistence kodu Infrastructure/ProjectApp.Infrastructure/Persistence altındadır.
