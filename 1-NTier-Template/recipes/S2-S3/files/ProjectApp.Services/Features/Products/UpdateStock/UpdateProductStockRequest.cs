namespace ProjectApp.Services.Features.Products.UpdateStock;

/// <summary>
/// Stoku verilen değere ayarlar (artış/azalış değil, yeni stok miktarı).
/// <paramref name="RowVersion"/>: ürün GET ile okunurken dönen değer; kayıt bu arada değiştiyse 409 döner (kayıp güncelleme önlenir).
/// </summary>
public record UpdateProductStockRequest(Guid ProductId, int Quantity, byte[] RowVersion);
